using Dalamud.Game.Chat;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace EorzeaEventsPlugin.Chat;

/// <summary>
/// Affichage du nom RP à la place du nom de personnage, dans le chat.
///
/// Purement local, comme la mise en couleur : le message part avec le nom du
/// jeu, et c'est la ligne reçue qui est réécrite avant d'atteindre l'écran.
///
/// Source unique : le cache des disponibilités, celui-là même qui alimente la
/// page « Autour de moi » et l'infobulle. Rien n'est demandé au serveur à la
/// réception d'un message, et ce n'est pas une question de performance : une
/// requête par réplique dirait au serveur qui parle à qui, et transformerait
/// l'écoute d'une conversation en relevé. Corollaire assumé : un joueur qui n'a
/// pas consenti à figurer dans la liste publique garde simplement son nom de
/// personnage.
/// </summary>
internal static class RpNameSubstitution
{
    public static void Apply(IHandleableChatMessage message)
    {
        var config = Plugin.Config;
        if (!config.ChatRpNames) return;

        var sender = message.Sender;
        if (sender.Payloads.Count == 0) return;

        if (Identify(sender) is not { } speaker)
        {
            // Les traces ne coûtent rien tant que le module est éteint : tout ce
            // qui précède est déjà sorti pour un chat ordinaire.
            Plugin.Log.Debug("[NomsRP] Locuteur non identifié : « {0} »", sender.TextValue ?? string.Empty);
            return;
        }

        var entry  = Resolve(speaker.Name, speaker.World);
        var rpName = entry?.Profile?.RpName?.Trim();

        Plugin.Log.Debug("[NomsRP] {0}@{1} : {2}",
                         speaker.Name, speaker.World,
                         entry == null
                             ? "aucune entrée de disponibilité"
                             : $"entrée trouvée sur {entry.Server}, nom RP « {rpName ?? string.Empty} »");

        // Une fiche sans nom RP, ou dont le nom RP est celui du personnage,
        // n'a rien à substituer.
        if (string.IsNullOrEmpty(rpName)
            || string.Equals(rpName, speaker.Name, StringComparison.Ordinal)) return;

        var color   = ResolveColor(entry!.Profile!.AccentColor);
        var result  = new List<Payload>(sender.Payloads.Count + 3);
        var applied = false;

        foreach (var payload in sender.Payloads)
        {
            // Le lien de joueur est laissé intact : c'est lui qui porte le clic
            // droit sur le nom. Seul le texte affiché change, ce qui garde le
            // menu contextuel branché sur le vrai personnage.
            if (applied || payload is not TextPayload { Text: { Length: > 0 } text })
            {
                result.Add(payload);
                continue;
            }

            var at = text.IndexOf(speaker.Name, StringComparison.Ordinal);
            if (at < 0)
            {
                result.Add(payload);
                continue;
            }

            if (at > 0) result.Add(new TextPayload(text[..at]));

            result.Add(new UIForegroundPayload(color));
            result.Add(new TextPayload(rpName));
            result.Add(new UIForegroundPayload(ChatPalette.Off));

            var after = at + speaker.Name.Length;
            if (after < text.Length) result.Add(new TextPayload(text[after..]));

            applied = true;
        }

        if (applied) message.Sender = new SeString(result);
        else Plugin.Log.Debug("[NomsRP] {0} introuvable dans l'expéditeur, ligne laissée telle quelle.",
                              speaker.Name);
    }

    /// <summary>
    /// Entrée de disponibilité correspondant au locuteur.
    ///
    /// Deux recours, dans cet ordre. Le couple nom + monde d'abord, seul
    /// identifiant réellement sûr. Puis, s'il ne donne rien, le nom seul, à la
    /// condition stricte qu'une unique entrée de la liste le porte : le monde
    /// déduit d'un message peut être faux (nom arrivé nu, joueur en voyage,
    /// serveur renommé), alors qu'un nom unique dans la liste ne laisse aucune
    /// place au doute.
    ///
    /// Deux homonymes sur des mondes différents laissent au contraire le nom
    /// intact, sans exception : afficher le nom RP du mauvais joueur en pleine
    /// scène est une erreur bien plus coûteuse qu'un nom non substitué, que
    /// personne ne remarque. On ne devine jamais.
    /// </summary>
    private static Api.RpAvailabilityEntryDto? Resolve(string name, string world)
    {
        if (Plugin.FindAvailableEntry(name, world) is { } exact) return exact;

        Api.RpAvailabilityEntryDto? single = null;

        foreach (var candidate in Plugin.AvailableEntries)
        {
            if (!string.Equals(candidate.CharacterName, name, StringComparison.Ordinal)) continue;
            if (single != null)
            {
                Plugin.Log.Debug("[NomsRP] {0} : plusieurs entrées homonymes, nom laissé intact.", name);
                return null;
            }
            single = candidate;
        }

        if (single != null)
            Plugin.Log.Debug("[NomsRP] {0} : repli sur l'unique entrée portant ce nom ({1}).",
                             name, single.Server);

        return single;
    }

    /// <summary>
    /// Nom et monde de celui qui parle.
    ///
    /// Le lien de joueur n'accompagne le nom que pour un interlocuteur d'un
    /// autre monde : sur le sien, le jeu se contente du texte. D'où le repli sur
    /// le monde d'origine du personnage local, qui est justement le seul cas où
    /// le nom arrive nu.
    /// </summary>
    private static (string Name, string World)? Identify(SeString sender)
    {
        foreach (var payload in sender.Payloads)
        {
            if (payload is not PlayerPayload player) continue;

            var world = player.World.ValueNullable?.Name.ToString();
            if (!string.IsNullOrEmpty(player.PlayerName) && !string.IsNullOrEmpty(world))
                return (player.PlayerName, world);
        }

        var text = sender.TextValue?.Trim();
        if (string.IsNullOrEmpty(text)) return null;

        // Le chat préfixe parfois le nom d'un caractère de rôle ou d'un
        // symbole de canal, qui ne fait pas partie du nom.
        text = new string([.. text.Where(c => char.IsLetter(c) || c == '\'' || c == '-' || c == ' ')]).Trim();
        if (text.Length == 0) return null;

        // Monde d'ORIGINE et non monde courant : les disponibilités sont
        // indexées sur le serveur d'appartenance, et un joueur en voyage, cas
        // ordinaire dès qu'on rejoint une maison, ne correspondrait plus à rien.
        var home = Plugin.HomeWorldName();
        return string.IsNullOrEmpty(home) ? null : (text, home);
    }

    /// <summary>
    /// Couleur d'accent de la fiche, ramenée à la palette du jeu. Le chat ne
    /// sait pas afficher une couleur libre, mais s'approcher du choix de
    /// l'auteur vaut mieux que l'ignorer.
    ///
    /// Aucun réglage local ne vient s'y substituer : la couleur d'un nom RP
    /// appartient à celui qui le porte. Une fiche sans accent reprend la teinte
    /// du plugin.
    /// </summary>
    private static ushort ResolveColor(string? accent)
    {
        if (Ui.Theme.TryParseHex(accent) is { } parsed)
            return ChatPalette.Nearest(Ui.Theme.EnsureReadable(parsed));

        return ChatPalette.Nearest(ChatPalette.NameDefault);
    }
}
