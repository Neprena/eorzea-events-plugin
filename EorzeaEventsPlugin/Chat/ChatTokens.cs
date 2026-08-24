using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;

namespace EorzeaEventsPlugin.Chat;

/// <summary>
/// Jetons de saisie : écrire « %xt » et obtenir le nom RP de sa cible.
///
/// Contrairement au reste du module, ceci touche à ce que l'on écrit et non à
/// ce que l'on reçoit. La substitution est donc explicite et manuelle : elle
/// répond à une commande, remplit le presse-papiers et n'envoie rien. Aucun
/// crochet sur la saisie du jeu, aucune ligne postée à la place du joueur :
/// les règles de publication de Dalamud interdisent d'automatiser l'envoi de
/// messages, et une aide au RP n'a aucune raison d'aller sur ce terrain.
///
/// Le texte substitué est aussi affiché en écho local, pour qu'on le relise
/// avant de le coller.
/// </summary>
internal static class ChatTokens
{
    /// <summary>
    /// Texte en attente de copie. Le presse-papiers passe par ImGui, qui n'est
    /// utilisable que pendant le rendu : une commande s'exécute sur le fil du
    /// jeu, où le contexte n'est pas actif.
    /// </summary>
    private static string? _pending;

    /// <summary>Jetons reconnus, du plus long au plus court.</summary>
    // L'ordre fait tout : « %xt » remplacerait le début de « %xtf » et laisserait
    // un « f » orphelin derrière lui.
    private static readonly string[] Tokens =
        ["%xtf", "%xtl", "%xt", "%xpf", "%xpl", "%xp"];

    public static void Run(string text)
    {
        var l = Plugin.L;

        if (string.IsNullOrWhiteSpace(text))
        {
            Plugin.ChatGui.Print(new SeStringBuilder()
                .AddUiForeground(32).AddText("[Eorzea Events] ").AddUiForegroundOff()
                .AddText(l.ChatTokensUsage)
                .Build());
            return;
        }

        var result = Substitute(text);
        _pending   = result;

        Plugin.ChatGui.Print(new SeStringBuilder()
            .AddUiForeground(32).AddText("[Eorzea Events] ").AddUiForegroundOff()
            .AddText(string.Format(l.ChatTokensCopied, result))
            .Build());
    }

    /// <summary>
    /// Remplace les jetons par les noms connus. Un nom RP inconnu retombe sur le
    /// nom du personnage : une réplique amputée serait pire qu'un nom de jeu.
    /// </summary>
    public static string Substitute(string text)
    {
        var target = Names(TargetName());
        var self   = Names(SelfName());

        foreach (var token in Tokens)
        {
            if (!text.Contains(token, StringComparison.OrdinalIgnoreCase)) continue;

            var value = token switch
            {
                "%xtf" => target.First,
                "%xtl" => target.Last,
                "%xt"  => target.Full,
                "%xpf" => self.First,
                "%xpl" => self.Last,
                _      => self.Full,
            };

            text = text.Replace(token, value, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }

    /// <summary>
    /// Copie différée, appelée à chaque rendu. Sans texte en attente, elle ne
    /// fait rien : c'est le prix d'un accès au presse-papiers qui n'est valable
    /// qu'ici.
    /// </summary>
    public static void FlushClipboard()
    {
        if (_pending is not { Length: > 0 } text) return;
        _pending = null;

        try { ImGui.SetClipboardText(text); }
        catch (Exception ex) { Plugin.Log.Warning(ex, "Copie dans le presse-papiers impossible."); }
    }

    /// <summary>
    /// Nom RP de la cible, ou son nom de personnage. Même source que le reste du
    /// module : le cache des disponibilités, jamais une requête au serveur.
    /// </summary>
    private static string? TargetName()
    {
        if (Plugin.TargetManager.Target is not IPlayerCharacter player) return null;

        var name  = player.Name.TextValue;
        var world = player.HomeWorld.ValueNullable?.Name.ToString();
        var entry = Plugin.FindAvailableEntry(name, world);
        var rp    = entry?.Profile?.RpName?.Trim();

        return string.IsNullOrEmpty(rp) ? name : rp;
    }

    /// <summary>Son propre nom RP, lu dans le cache de sa fiche.</summary>
    private static string? SelfName()
    {
        if (Plugin.CurrentCharacter is not { } character) return null;

        var rp = Plugin.Config.FindProfile(character.Name, character.WorldId)?.RpName?.Trim();
        return string.IsNullOrEmpty(rp) ? character.Name : rp;
    }

    /// <summary>
    /// Découpe en prénom et nom. Un nom RP est libre : il peut n'avoir qu'un
    /// mot, ou en avoir quatre. Le premier mot fait le prénom, le reste le nom,
    /// et un nom d'un seul mot vaut pour les deux.
    /// </summary>
    private static (string Full, string First, string Last) Names(string? full)
    {
        if (string.IsNullOrWhiteSpace(full)) return (string.Empty, string.Empty, string.Empty);

        full = full.Trim();
        var cut = full.IndexOf(' ');

        return cut < 0
            ? (full, full, full)
            : (full, full[..cut], full[(cut + 1)..].TrimStart());
    }
}
