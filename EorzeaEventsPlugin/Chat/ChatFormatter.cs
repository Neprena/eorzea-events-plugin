using System.Text.RegularExpressions;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace EorzeaEventsPlugin.Chat;

/// <summary>
/// Mise en couleur des conventions d'écriture du RP dans le chat.
///
/// Tout se passe à la réception et sur la machine du lecteur : le message parti
/// n'est jamais modifié, et un interlocuteur sans le plugin voit exactement ce
/// qui a été tapé. C'est la seule forme acceptable pour une aide de ce genre,
/// puisqu'elle n'impose rien à personne et ne demande à personne de s'équiper.
///
/// Un motif ouvert dans un message et refermé dans le suivant n'est pas
/// reconstitué, volontairement : le jeu découpe les longues répliques à sa
/// guise, et recoller les morceaux ferait déteindre une couleur sur des lignes
/// étrangères au premier découpage inattendu.
/// </summary>
internal sealed class ChatFormatter
{
    /// <summary>Segment de texte à colorer, en indices sur la chaîne d'origine.</summary>
    private readonly record struct Highlight(int Start, int End, ushort Color);

    // Motifs compilés une fois pour toutes : ils s'appliquent à chaque ligne du
    // chat, y compris aux combats, et les reconstruire à chaque message
    // coûterait bien plus cher que la mise en couleur elle-même.
    //
    // Aucun motif ne traverse une fin de ligne : une paire de symboles laissée
    // ouverte ne doit pas avaler la suite du message.
    private static readonly Regex EmoteStars = new(
        @"\*\*[^*\r\n]+\*\*|\*[^*\r\n]+\*", RegexOptions.Compiled);

    private static readonly Regex EmoteAngles = new(
        @"<[^<>\r\n]+>", RegexOptions.Compiled);

    private static readonly Regex OutOfCharacter = new(
        @"\(\([^()\r\n]*\)\)|\([^()\r\n]+\)", RegexOptions.Compiled);

    private static readonly Regex Speech = new(
        @"«[^«»\r\n]+»|""[^""\r\n]+""", RegexOptions.Compiled);

    /// <summary>
    /// Point d'entrée unique de l'écoute du chat : la mise en couleur et le
    /// remplacement du nom retouchent le même message et n'ont donc rien à
    /// gagner à s'abonner séparément.
    /// </summary>
    public void OnChatMessage(IHandleableChatMessage message)
    {
        var config = Plugin.Config;
        if (!config.ChatFormatEnabled) return;
        if (!IsChannelEnabled(message.LogKind, config)) return;

        try
        {
            RpNameSubstitution.Apply(message);
            Colorize(message, config);
        }
        catch (Exception ex)
        {
            // Une exception qui remonte d'ici traverserait le traitement du
            // chat par le jeu : mieux vaut une ligne non colorée qu'une ligne
            // perdue.
            Plugin.Log.Error(ex, "Mise en forme du chat impossible.");
        }
    }

    private static void Colorize(IHandleableChatMessage message, Configuration config)
    {
        if (!config.ChatFormatEmote && !config.ChatFormatOoc && !config.ChatFormatSpeech) return;

        var payloads = message.Message.Payloads;
        var result   = new List<Payload>(payloads.Count + 8);
        var changed  = false;

        foreach (var payload in payloads)
        {
            // Seuls les segments de texte sont touchés. Les autres portent des
            // charges utiles du jeu, liens d'objet, mentions de joueur, points
            // de carte : les reconstruire casserait leurs infobulles et leur
            // menu contextuel.
            if (payload is not TextPayload { Text: { Length: > 0 } text })
            {
                result.Add(payload);
                continue;
            }

            var highlights = Collect(text, config);
            if (highlights.Count == 0)
            {
                result.Add(payload);
                continue;
            }

            changed = true;
            var cursor = 0;

            foreach (var highlight in highlights)
            {
                if (highlight.Start > cursor)
                    result.Add(new TextPayload(text[cursor..highlight.Start]));

                result.Add(new UIForegroundPayload(highlight.Color));
                result.Add(new TextPayload(text[highlight.Start..highlight.End]));
                // Clé 0 : retour à la couleur du canal. Le chat n'empile pas
                // les couleurs, il n'y a donc rien à restaurer de plus.
                result.Add(new UIForegroundPayload(ChatPalette.Off));

                cursor = highlight.End;
            }

            if (cursor < text.Length)
                result.Add(new TextPayload(text[cursor..]));
        }

        // Réécrire un message inchangé le marquerait comme modifié auprès des
        // autres plugins, qui en tireraient de mauvaises conclusions.
        if (changed) message.Message = new SeString(result);
    }

    /// <summary>
    /// Segments à colorer dans une chaîne, sans chevauchement et dans l'ordre.
    /// </summary>
    private static List<Highlight> Collect(string text, Configuration config)
    {
        var found = new List<Highlight>();

        if (config.ChatFormatEmote)
        {
            var color = ChatPalette.Resolve(config.ChatEmoteColor, ChatPalette.EmoteDefault);

            if (config.ChatEmoteStyle != ChatEmoteStyle.Angles)
                Add(found, EmoteStars, text, color);
            if (config.ChatEmoteStyle != ChatEmoteStyle.Stars)
                Add(found, EmoteAngles, text, color);
        }

        if (config.ChatFormatOoc)
            Add(found, OutOfCharacter, text, ChatPalette.Resolve(config.ChatOocColor, ChatPalette.OocDefault));
        if (config.ChatFormatSpeech)
            Add(found, Speech, text, ChatPalette.Resolve(config.ChatSpeechColor, ChatPalette.SpeechDefault));

        if (found.Count < 2) return found;

        found.Sort(static (a, b) => a.Start != b.Start
            ? a.Start.CompareTo(b.Start)
            : b.End.CompareTo(a.End));

        // Deux motifs peuvent se croiser, par exemple une parenthèse ouverte
        // dans une emote. Le premier commencé l'emporte, et le plus long à
        // position égale : c'est la lecture qu'en fait un humain.
        var kept = new List<Highlight>(found.Count);
        var end  = 0;

        foreach (var highlight in found)
        {
            if (highlight.Start < end) continue;
            kept.Add(highlight);
            end = highlight.End;
        }

        return kept;
    }

    private static void Add(List<Highlight> found, Regex pattern, string text, ushort color)
    {
        foreach (Match match in pattern.Matches(text))
            found.Add(new Highlight(match.Index, match.Index + match.Length, color));
    }

    /// <summary>
    /// Canaux traités. Tout ce qui n'est pas listé est ignoré : le chat
    /// charrie surtout des messages du jeu, et un astérisque dans un nom d'objet
    /// n'est pas une emote.
    /// </summary>
    private static bool IsChannelEnabled(XivChatType type, Configuration config) => type switch
    {
        XivChatType.Say         => config.ChatChannelSay,
        XivChatType.Shout       => config.ChatChannelShout,
        XivChatType.Yell        => config.ChatChannelYell,
        XivChatType.TellIncoming or XivChatType.TellOutgoing => config.ChatChannelTell,
        XivChatType.Party or XivChatType.CrossParty or XivChatType.Alliance => config.ChatChannelParty,
        >= XivChatType.Ls1 and <= XivChatType.Ls8 => config.ChatChannelLinkshell,
        XivChatType.CrossLinkShell1 => config.ChatChannelLinkshell,
        >= XivChatType.CrossLinkShell2 and <= XivChatType.CrossLinkShell8 => config.ChatChannelLinkshell,
        XivChatType.FreeCompany => config.ChatChannelFreeCompany,
        XivChatType.CustomEmote or XivChatType.StandardEmote => config.ChatChannelEmote,
        _ => false,
    };
}
