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
            Version: "2.6.2",
            TitleFr: "Codes de sync",
            TitleEn: "Sync codes",
            BodyFr: """
                    **Nouveautés**
                    - Les codes de sync (**Snowcloak**, **Umbra** et consorts) arrivent sur la fiche RP, avec le choix de qui peut les voir.
                    - Un clic copie le code depuis la fiche d'un autre joueur.
                    """,
            BodyEn: """
                    **What's new**
                    - Sync codes (**Snowcloak**, **Umbra** and others) are now on the RP profile, with a choice of who can see them.
                    - One click copies the code from another player's profile.
                    """),

        new(
            Version: "2.6.0",
            TitleFr: "Les fiches RP prennent des couleurs",
            TitleEn: "RP profiles get their colours",
            BodyFr: """
                    **Nouveautés**
                    - La **bannière**, la **couleur d'accent** et le **portrait** réglés sur le site s'affichent enfin sur la fiche.
                    - Les **adhérents** débloquent un cadre de portrait, un titre personnalisé sous le nom et son animation.
                    - L'**équipe du site** peut afficher son badge, désactivé par défaut.
                    - La fiche se remet à jour d'elle-même après une modification faite sur le site. Un rafraîchissement manuel reste possible.
                    """,
            BodyEn: """
                    **What's new**
                    - The **banner**, **accent colour** and **portrait** set on the website now show on the profile.
                    - **Members** unlock a portrait frame, a custom title under the name and its animation.
                    - The **site team** can display its badge, off by default.
                    - The profile refreshes itself after an edit made on the website. Manual refresh is still available.
                    """),

        new(
            Version: "2.5.2",
            TitleFr: "Plus de faux « lien expiré » après un téléport",
            TitleEn: "No more false \"link expired\" after a teleport",
            BodyFr: """
                    **Corrections**
                    - Plus de faux **« lien expiré »** après un téléport : le jeton survit aux écrans de chargement.
                    - Une révocation n'est signalée qu'après plusieurs refus consécutifs, un incident réseau passager ne suffit plus.
                    """,
            BodyEn: """
                    **Fixes**
                    - No more false **"link expired"** after a teleport: the token survives loading screens.
                    - A revocation is only reported after several consecutive rejections, a passing network hiccup no longer counts.
                    """),

        new(
            Version: "2.5.1",
            TitleFr: "L'historique des nouveautés, rattrapé",
            TitleEn: "The what's new history, caught up",
            BodyFr: """
                    **Corrections**
                    - Les versions 2.3.1 à 2.5.0 étaient sorties **sans notes** : tout ce qui a été manqué est déplié ci-dessous.
                    - La fenêtre s'ouvre dès qu'une version non lue a des notes, même si la version installée n'en a pas.
                    """,
            BodyEn: """
                    **Fixes**
                    - Versions 2.3.1 to 2.5.0 shipped **without notes**: everything that was missed is expanded below.
                    - The window now opens as soon as an unread version has notes, even when the installed one has none.
                    """),

        new(
            Version: "2.5.0",
            TitleFr: "Compteur « sur place » partout",
            TitleEn: "\"On site\" count everywhere",
            BodyFr: """
                    **Nouveautés**
                    - Le compteur **sur place** d'un RP ouvert fonctionne dans toutes les zones, plus seulement en quartier résidentiel.
                    - L'**instance publique** entre dans le décompte : deux joueurs dans « Thanalan occidental 1 » et « 2 » ne sont plus comptés ensemble.
                    """,
            BodyEn: """
                    **What's new**
                    - The **on site** count for an open RP session works in every zone, not only in housing wards.
                    - The **public instance** counts: two players in "Western Thanalan 1" and "2" are no longer counted together.
                    """),

        new(
            Version: "2.4.1",
            TitleFr: "Une panne ne ressemble plus à une liste vide",
            TitleEn: "An outage no longer looks like an empty list",
            BodyFr: """
                    **Nouveautés**
                    - Les joueurs détectés sur place, l'avertissement de contenu sensible et le numéro d'appartement s'affichent enfin.

                    **Corrections**
                    - Site injoignable ou erreur serveur : les listes **gardent leur contenu** et proposent de réessayer, au lieu de paraître vides.
                    - L'enregistrement d'une fiche dit enfin s'il a réussi, et un champ vidé est réellement effacé.
                    - Douze thèmes et neuf races restaient en français dans la version anglaise.
                    """,
            BodyEn: """
                    **What's new**
                    - Players detected on site, the sensitive content warning and the apartment number finally appear.

                    **Fixes**
                    - Site unreachable or server error: lists now **keep their content** and offer to retry, instead of looking empty.
                    - Saving a profile finally reports success, and a cleared field is really erased.
                    - Twelve themes and nine races stayed in French in the English build.
                    """),

        new(
            Version: "2.4.0",
            TitleFr: "Amis RP et confidentialité",
            TitleEn: "RP friends and privacy",
            BodyFr: """
                    **Nouveautés**
                    - Nouvel onglet **Amis RP** : les personnages à qui la fiche est ouverte, avec marqueur de réciprocité et note privée.
                    - L'ajout se fait par **clic droit sur un joueur**, sans notification.
                    - Nouvel échelon de visibilité **« Mes amis RP »**, avec un aperçu de ce qu'un ami voit.

                    **Corrections**
                    - Republier une fiche depuis le jeu n'écrase plus les réglages faits sur le site : page web, indexation et sections masquées restent en place.
                    """,
            BodyEn: """
                    **What's new**
                    - New **RP friends** tab: the characters the profile is open to, with a mutual marker and a private note.
                    - Adding a friend happens by **right-clicking a player**, with no notification sent.
                    - New **"My RP friends"** visibility tier, with a preview of what a friend sees.

                    **Fixes**
                    - Republishing a profile from the game no longer overwrites settings made on the website: web page, search indexing and hidden sections stay as they were.
                    """),

        new(
            Version: "2.3.2",
            TitleFr: "Accroches et thèmes conservés",
            TitleEn: "Hooks and themes preserved",
            BodyFr: """
                    **Corrections**
                    - L'assistant de première configuration effaçait les **accroches**, les **thèmes recherchés** et les **thèmes évités** : il ne touche plus qu'à ce qu'il affiche.
                    """,
            BodyEn: """
                    **Fixes**
                    - The first-time setup wizard erased **hooks**, **sought themes** and **avoided themes**: it now only touches what it displays.
                    """),

        new(
            Version: "2.3.1",
            TitleFr: "Fiche préservée et portrait agrandi",
            TitleEn: "Profile preserved, larger portrait",
            BodyFr: """
                    **Nouveautés**
                    - Portrait plus grand dans la fiche comme dans les listes, et cliquable pour l'afficher en grand.

                    **Corrections**
                    - La **disponibilité RP** n'efface plus ce qui ne s'édite que sur le site : le reste de la fiche est laissé intact.
                    - Elle affiche une erreur au lieu de rester allumée à tort quand le serveur refuse.
                    - Plus de seconde barre de défilement dans les fenêtres à pied fixe.
                    """,
            BodyEn: """
                    **What's new**
                    - Larger portrait in the profile and in lists, clickable to view it full size.

                    **Fixes**
                    - **RP availability** no longer wipes what can only be edited on the website: the rest of the profile is left intact.
                    - It shows an error instead of wrongly staying on when the server refuses.
                    - No more second scrollbar in windows with a fixed footer.
                    """),

        new(
            Version: "2.3.0",
            TitleFr: "Nouvelle interface, fiches RP et confidentialité",
            TitleEn: "A new interface, RP profiles and privacy",
            BodyFr: """
                    **Nouveautés**
                    - Interface repensée : navigation par menu latéral, cartes plus lisibles, et textes du site rendus avec leur mise en forme.
                    - Une **fiche RP par personnage**, avec un partage réglé section par section et le référencement par les moteurs désactivé par défaut.
                    - Page **Autour de moi** et clic droit sur un joueur pour consulter la fiche des autres.
                    - Événements regroupés par jour, avec recherche, filtres et bouton **Y aller** via Lifestream.

                    **Corrections**
                    - Zone correcte pour les RP démarrés en quartier résidentiel, et correction d'un plantage possible au changement de zone.
                    """,
            BodyEn: """
                    **What's new**
                    - Redesigned interface: sidebar navigation, clearer cards, and website text rendered with its formatting.
                    - One **RP profile per character**, shared section by section, with search engine listing off by default.
                    - New **Around me** page, plus right-click on a player to view other profiles.
                    - Events grouped by day, with search, filters and a **Go there** button via Lifestream.

                    **Fixes**
                    - Correct zone for RP sessions started in a housing ward, and a possible crash when changing zone.
                    """),

        new(
            Version: "2.2.0",
            TitleFr: "Un jeton par personnage",
            TitleEn: "One token per character",
            BodyFr: """
                    **Nouveautés**
                    - **Un jeton d'accès par personnage** au lieu d'un seul pour tout le compte, choisi automatiquement selon le personnage connecté.
                    - Gestion de plusieurs personnages liés depuis les paramètres.
                    - Paliers de fidélité et suivi de présence dans les établissements.

                    **Corrections**
                    - Les événements annulés disparaissaient de la liste sans explication.
                    """,
            BodyEn: """
                    **What's new**
                    - **One access token per character** instead of a single account-wide one, picked automatically for the character in play.
                    - Several linked characters can be managed from the settings.
                    - Loyalty tiers and presence tracking in venues.

                    **Fixes**
                    - Cancelled events vanished from the list without explanation.
                    """),

        new(
            Version: "2.1.0",
            TitleFr: "Disponibilité RP et première fiche",
            TitleEn: "RP availability and the first profile",
            BodyFr: """
                    **Nouveautés**
                    - La **disponibilité RP** devient un statut permanent plutôt qu'une annonce ponctuelle.
                    - Première version de la **fiche RP** : niveau de jeu, mode d'approche, langues.
                    - Les informations de logement accompagnent la présence, pour être retrouvé dans un quartier résidentiel.
                    """,
            BodyEn: """
                    **What's new**
                    - **RP availability** becomes a lasting status rather than a one-off announcement.
                    - First version of the **RP profile**: play level, approach style, languages.
                    - Housing details travel with presence, making a character findable inside a residential ward.
                    """),

        new(
            Version: "2.0.0",
            TitleFr: "Refonte de la configuration",
            TitleEn: "Setup rework",
            BodyFr: """
                    **Nouveautés**
                    - Fenêtre de configuration et composants d'interface repensés : la liaison du compte est plus claire à la première utilisation.
                    """,
            BodyEn: """
                    **What's new**
                    - Reworked setup window and interface components: linking an account is clearer on first use.
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
