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
            Version: "2.5.1",
            TitleFr: "L'historique des nouveautés, rattrapé",
            TitleEn: "The what's new history, caught up",
            BodyFr: """
                    - Les versions 2.3.1 à 2.5.0 sont sorties **sans notes** : cette fenêtre ne s'ouvrait plus après une mise à jour, et l'encart du Plugin Installer montrait encore le texte de la 2.3.0. Tout ce qui a été manqué est déplié ci-dessous.
                    - La fenêtre s'ouvre désormais dès qu'une version non lue a des notes, même si celle qui est installée n'en a pas : un oubli ne peut plus faire disparaître l'historique.
                    """,
            BodyEn: """
                    - Versions 2.3.1 through 2.5.0 shipped **without notes**: this window no longer opened after an update, and the Plugin Installer panel still showed the 2.3.0 text. Everything you missed is expanded below.
                    - The window now opens as soon as an unread version has notes, even when the installed one has none: an oversight can no longer make the history vanish.
                    """),

        new(
            Version: "2.5.0",
            TitleFr: "Compteur « sur place » partout",
            TitleEn: "\"On site\" count everywhere",
            BodyFr: """
                    - Le compteur des personnes **sur place** d'un RP ouvert fonctionne maintenant dans toutes les zones, et plus seulement dans les quartiers résidentiels : le plugin transmet ta position sur la carte.
                    - L'**instance publique** est prise en compte : deux joueurs dans « Thanalan occidental 1 » et « 2 » ne sont plus comptés ensemble alors qu'ils ne peuvent pas se voir.
                    """,
            BodyEn: """
                    - The **on site** count for an open RP session now works in every zone, not only in housing wards: the plugin sends your position on the map.
                    - The **public instance** is taken into account: two players in "Western Thanalan 1" and "2" are no longer counted together when they cannot see each other.
                    """),

        new(
            Version: "2.4.1",
            TitleFr: "Une panne ne ressemble plus à une liste vide",
            TitleEn: "An outage no longer looks like an empty list",
            BodyFr: """
                    ## Erreurs

                    - Site injoignable, erreur serveur ou délai dépassé : les listes **gardent leur contenu** et proposent de réessayer, au lieu d'afficher « rien à afficher ».
                    - Une panne réseau ne fait plus disparaître le bouton de reprise d'une session en cours, ce qui laissait croire qu'elle était terminée.
                    - L'enregistrement d'une fiche dit enfin s'il a réussi, et pourquoi il a échoué. L'ajout d'un ami distingue les cas : personnage sans fiche visible en jeu, liste pleine, personnage non lié, échec technique.

                    ## Version anglaise

                    - Douze thèmes et neuf races s'affichaient en français quel que soit le réglage de langue.

                    ## Affichés enfin

                    - Joueurs détectés sur place, pour les sessions RP comme pour les événements.
                    - Avertissement de contenu sensible dans le détail d'un établissement.
                    - Citation dans « Autour de moi », et numéro d'appartement dans l'adresse annoncée en chat.

                    ## Corrections

                    - Un champ vidé dans la fiche est désormais réellement effacé.
                    - En quittant une maison, ta session restait annoncée à l'adresse du logement.
                    - Barre de titre : les boutons de repli et de fermeture ne sont plus collés l'un à l'autre.
                    """,
            BodyEn: """
                    ## Errors

                    - Site unreachable, server error or timeout: lists now **keep their content** and offer to retry, instead of showing "nothing to display".
                    - A network outage no longer hides the resume button of a running session, which made it look finished.
                    - Saving a profile finally reports success, and why it failed. Adding a friend tells the cases apart: character with no profile visible in game, list full, character not linked, technical failure.

                    ## English build

                    - Twelve themes and nine races were shown in French whatever the language setting.

                    ## Shown at last

                    - Players detected on site, for RP sessions as well as events.
                    - Sensitive content warning in a venue's details.
                    - Quote in "Around me", and apartment number in the address announced in chat.

                    ## Fixes

                    - A field cleared in your profile is now actually erased.
                    - When leaving a house, your session stayed announced at the housing address.
                    - Title bar: the collapse and close buttons are no longer stuck together.
                    """),

        new(
            Version: "2.4.0",
            TitleFr: "Amis RP et confidentialité",
            TitleEn: "RP friends and privacy",
            BodyFr: """
                    ## Amis RP

                    - Nouvel onglet **Amis RP** : les personnages à qui ta fiche est ouverte, avec marqueur de réciprocité, note privée et retrait à deux clics.
                    - Ajout par **clic droit sur un joueur**, depuis « Autour de moi » ou depuis la fiche consultée. Le geste ouvre ta propre fiche : il ne donne accès à rien d'autre et n'est pas notifié.
                    - Nouvel échelon de visibilité **« Mes amis RP »**, avec un préréglage qui bascule d'un coup les sections réservées, et un aperçu à deux onglets pour constater ce qu'un ami voit.

                    ## Réglages du site préservés

                    - Trois chemins par lesquels le plugin pouvait écraser des réglages faits sur le site sont fermés : republier une fiche ne réactive plus la page web ni l'indexation, et ne remet plus en clair des sections masquées.
                    - Les valeurs par défaut de section n'étaient plus les bonnes depuis un ajout au vocabulaire.

                    ## Affichage

                    - Quatre champs servis par le serveur mais dessinés nulle part apparaissent enfin : thème musical, lien externe, prise de contact et durée des scènes.
                    - L'assistant de première configuration est retiré.
                    """,
            BodyEn: """
                    ## RP friends

                    - New **RP friends** tab: the characters your profile is open to, with a mutual marker, a private note and two-click removal.
                    - Add by **right-clicking a player**, from "Around me" or from the profile you are viewing. The action opens your own profile: it grants nothing else and is not notified.
                    - New **"My RP friends"** visibility tier, with a preset that flips the reserved sections at once, and a two-tab preview to see what a friend sees.

                    ## Website settings preserved

                    - Three paths through which the plugin could overwrite settings made on the website are closed: republishing a profile no longer re-enables the web page or search indexing, and no longer reveals hidden sections.
                    - Section defaults had been wrong since an addition to the vocabulary.

                    ## Display

                    - Four fields served by the server but drawn nowhere finally appear: musical theme, external link, how to get in touch and scene length.
                    - The first-time setup wizard has been removed.
                    """),

        new(
            Version: "2.3.2",
            TitleFr: "Accroches et thèmes conservés",
            TitleEn: "Hooks and themes preserved",
            BodyFr: """
                    - L'assistant de première configuration effaçait les **accroches**, les **thèmes recherchés** et les **thèmes évités** dès qu'on le traversait : il ne touche plus qu'à ce qu'il affiche.
                    """,
            BodyEn: """
                    - The first-time setup wizard erased your **hooks**, **sought themes** and **avoided themes** as soon as you went through it: it now only touches what it displays.
                    """),

        new(
            Version: "2.3.1",
            TitleFr: "Fiche préservée et portrait agrandi",
            TitleEn: "Profile preserved, larger portrait",
            BodyFr: """
                    - Se déclarer **disponible pour du RP** n'efface plus ce qui ne s'édite que sur le site : le reste de la fiche est laissé intact.
                    - La disponibilité se pilote depuis les quatre mêmes endroits (barre de statut, fiche RP, réglages, onglet RP ouvert), et affiche une erreur au lieu de rester allumée à tort quand le serveur refuse.
                    - Portrait plus grand dans la fiche comme dans les listes, et cliquable pour l'afficher en grand.
                    - Plus de seconde barre de défilement dans les fenêtres à pied fixe.
                    """,
            BodyEn: """
                    - Declaring yourself **available for RP** no longer wipes what can only be edited on the website: the rest of your profile is left intact.
                    - Availability is driven from the same four places (status bar, RP profile, settings, Open RP tab), and shows an error instead of wrongly staying on when the server refuses.
                    - Larger portrait in the profile and in lists, clickable to view it full size.
                    - No more second scrollbar in windows with a fixed footer.
                    """),

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
