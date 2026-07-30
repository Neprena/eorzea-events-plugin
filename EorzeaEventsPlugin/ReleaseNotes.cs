namespace EorzeaEventsPlugin;

/// <summary>
/// Notes d'une version, dans les deux langues.
///
/// Le corps est du Markdown, rendu par
/// <see cref="Ui.Components.MarkdownView"/> : titres, gras, listes et liens.
/// </summary>
internal sealed record ReleaseNote(
    string Version,
    string TitleFr, string TitleEn,
    string BodyFr,  string BodyEn)
{
    public string Title => Plugin.L.IsFrench ? TitleFr : TitleEn;
    public string Body  => Plugin.L.IsFrench ? BodyFr  : BodyEn;
}

/// <summary>
/// Nouveautés présentées au joueur après une mise à jour.
///
/// Le contenu est embarqué dans le binaire, comme le font les plugins de
/// l'écosystème (Penumbra, Simple Tweaks, Craftimizer) : il reste disponible
/// hors ligne et ne peut pas se désynchroniser de la version installée.
///
/// Les notes vivent ici plutôt que dans <see cref="Loc"/>, qui compte déjà
/// plusieurs centaines de propriétés obligatoires à renseigner deux fois.
///
/// L'historique s'arrête volontairement à la ligne 2.x. Les versions 1.x
/// relevaient surtout de la migration vers l'API 15 de Dalamud et de correctifs
/// de compatibilité : rien qu'un joueur ait vu, et les messages de commit de
/// l'époque ne permettent pas d'en tirer des notes honnêtes.
/// </summary>
internal static class ReleaseNotes
{
    /// <summary>
    /// Notes de version, la plus récente en tête. L'ordre compte : la fenêtre
    /// affiche l'historique dans cet ordre et déplie ce qui n'a pas été vu.
    /// </summary>
    public static readonly ReleaseNote[] All =
    [
        new(
            Version: "2.3.0",
            TitleFr: "Nouvelle interface, fiches RP et confidentialité",
            TitleEn: "A new interface, RP profiles and privacy",
            BodyFr: """
                    ## Une interface repensée

                    - Navigation par menu latéral : RP ouvert, Autour de moi, Événements, Fiche RP, Lieux et Paramètres.
                    - Nouvelle typographie et nouvelle palette : les cartes sont plus lisibles et mieux aérées.
                    - Les textes rédigés sur le site s'affichent enfin avec leur mise en forme : titres, gras, listes et liens.

                    ## Consulter la fiche des autres

                    - Nouvelle page **Autour de moi** : qui est disponible pour du RP, avec sa fiche en un clic.
                    - **Clic droit sur un joueur** disponible pour ouvrir sa fiche, depuis une plaque de nom, la liste d'équipe ou le chat.
                    - La fiche affichée montre désormais tout ce que son auteur partage : portrait, accroches, biographie, relations, traits physiques.
                    - Chaque fiche peut aussi avoir sa page sur le site, partageable hors du jeu.

                    ## Confidentialité

                    - Tu choisis **section par section** ce que tu montres : identité, accroches, traits physiques, appartenances, description, relations et limites.
                    - Trois réglages distincts : visible en jeu, page web partageable, référencée par les moteurs de recherche. Aucun n'implique les autres.
                    - Le référencement par les moteurs est **désactivé par défaut**, y compris pour les fiches déjà écrites.
                    - Ce qui n'est pas partagé n'est jamais envoyé aux autres joueurs, et pas seulement masqué à l'affichage.

                    ## Fiche RP

                    - Une fiche par personnage : plus de mélange entre tes différents rôles.
                    - Portrait du personnage, traits physiques, appartenances et relations.
                    - Disponibilité RP déclarée personnage par personnage.

                    ## Agenda et voyage

                    - Les événements sont regroupés par jour, avec une recherche et des filtres officiel / communauté.
                    - Bouton **Y aller** sur les événements, les RP ouverts et les fiches de lieu, quand Lifestream est installé.

                    ## Corrections

                    - Les RP ouverts à proximité sont aussi annoncés dans le chat du jeu.
                    - Zone correcte pour les RP démarrés en quartier résidentiel.
                    - Les noms de zone trop longs ne sont plus coupés au bord des cartes.
                    - Le bouton « Ignorer » des bandeaux d'alerte est enfin visible.
                    - Correction d'un plantage possible lors d'un changement de zone.
                    """,
            BodyEn: """
                    ## A redesigned interface

                    - Sidebar navigation: Open RP, Around me, Events, RP Profile, Venues and Settings.
                    - New typography and palette: cards are easier to read and better spaced.
                    - Text written on the website finally renders with its formatting: headings, bold, lists and links.

                    ## Viewing other players' profiles

                    - New **Around me** page: see who is available for RP, and open their profile in one click.
                    - **Right-click an available player** to open their profile, from a nameplate, the party list or the chat log.
                    - The profile view now shows everything its author shares: portrait, hooks, background, relationships, physical traits.
                    - Every profile can also have its own page on the website, shareable outside the game.

                    ## Privacy

                    - You choose **section by section** what you share: identity, hooks, physical traits, affiliations, description, relationships and limits.
                    - Three separate settings: visible in game, shareable web page, listed in search engines. None implies the others.
                    - Search engine listing is **off by default**, including for profiles written earlier.
                    - What you do not share is never sent to other players, not merely hidden from view.

                    ## RP profile

                    - One profile per character: your different roles no longer share a single sheet.
                    - Character portrait, physical traits, allegiances and relationships.
                    - RP availability declared per character.

                    ## Agenda and travel

                    - Events are grouped by day, with search and official / community filters.
                    - **Go there** button on events, open RP sessions and venue pages, when Lifestream is installed.

                    ## Fixes

                    - Nearby open RP sessions are also announced in the game chat.
                    - Correct zone for RP sessions started in a housing ward.
                    - Long zone names are no longer cut off at the edge of cards.
                    - The "Dismiss" button on alert banners is finally visible.
                    - Fixed a possible crash when changing zone.
                    """),

        new(
            Version: "2.2.0",
            TitleFr: "Un jeton par personnage",
            TitleEn: "One token per character",
            BodyFr: """
                    - **Un jeton d'accès par personnage** au lieu d'un seul pour tout le compte, meilleur pour l'anonymat comme pour la sécurité. Le plugin choisit automatiquement le bon selon le personnage connecté.
                    - Gestion de plusieurs personnages liés depuis les paramètres.
                    - Paliers de fidélité et suivi de présence dans les établissements.
                    - Les événements annulés apparaissent désormais dans la liste, au lieu de disparaître sans explication.
                    - Correction de la hauteur de défilement de la liste des sessions.
                    """,
            BodyEn: """
                    - **One access token per character** instead of a single account-wide one, better for both anonymity and security. The plugin automatically picks the right one for the character you are playing.
                    - Manage several linked characters from the settings.
                    - Loyalty tiers and presence tracking in venues.
                    - Cancelled events now appear in the list instead of vanishing without explanation.
                    - Fixed the scroll height of the session list.
                    """),

        new(
            Version: "2.1.0",
            TitleFr: "Disponibilité RP et première fiche",
            TitleEn: "RP availability and the first profile",
            BodyFr: """
                    - **Disponibilité RP**, devenue un statut permanent plutôt qu'une annonce ponctuelle : les autres rôlistes voient que tu es ouvert aux rencontres.
                    - Première version de la **fiche RP** : niveau de jeu, mode d'approche, langues.
                    - Les informations de logement accompagnent désormais ta présence, pour qu'on te trouve dans un quartier résidentiel.
                    - Invitations Discord normalisées.
                    """,
            BodyEn: """
                    - **RP availability**, now a lasting status rather than a one-off announcement: other roleplayers can see you are open to encounters.
                    - First version of the **RP profile**: play level, approach style, languages.
                    - Housing details now travel with your presence, so people can find you inside a residential ward.
                    - Discord invites normalised.
                    """),

        new(
            Version: "2.0.0",
            TitleFr: "Refonte de la configuration",
            TitleEn: "Setup rework",
            BodyFr: """
                    - Fenêtre de configuration et composants d'interface repensés, pour rendre la liaison du compte plus claire à la première utilisation.
                    """,
            BodyEn: """
                    - Reworked setup window and interface components, to make linking your account clearer on first use.
                    """),
    ];

    /// <summary>Notes de la version donnée, ou null si elle n'en a pas.</summary>
    public static ReleaseNote? For(string version)
    {
        var exact = Array.Find(All, note => note.Version == version);
#if DEBUG
        // Le .csproj n'est bumpé qu'au moment de la release : sur un build de
        // développement, la version installée ne correspond à aucune entrée.
        // Retomber sur la plus récente garde la fenêtre testable en jeu.
        return exact ?? (All.Length > 0 ? All[0] : null);
#else
        return exact;
#endif
    }

    /// <summary>
    /// Vrai si cette version est plus récente que celle déjà acquittée, donc à
    /// déplier dans l'historique.
    ///
    /// Une version illisible est considérée comme nouvelle : mieux vaut montrer
    /// une entrée de trop que d'en cacher une que le joueur n'a jamais vue.
    /// </summary>
    public static bool IsUnseen(string version, string? lastSeen)
    {
        if (string.IsNullOrWhiteSpace(lastSeen)) return true;
        if (!Version.TryParse(version, out var current)) return true;
        return !Version.TryParse(lastSeen, out var seen) || current > seen;
    }
}
