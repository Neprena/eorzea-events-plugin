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
/// Le corps suit les préceptes de Keep a Changelog 1.1.0
/// (https://keepachangelog.com/fr/1.1.0/) : un en-tête de rubrique en gras par
/// type de changement, dans l'ordre **Ajouté**, **Modifié**, **Déprécié**,
/// **Supprimé**, **Corrigé**, **Sécurité** (**Added**, **Changed**,
/// **Deprecated**, **Removed**, **Fixed**, **Security**), suivi d'une liste.
/// Seules les rubriques utiles à la version sont écrites. Les en-têtes restent
/// du gras et non des titres « ## » : la fenêtre est rendue par ImGui, où un
/// titre de niveau 2 casserait la mise en page.
///
/// Les entrées antérieures à la 2.6.0 gardent les anciennes rubriques
/// « Nouveautés »/« Corrections » : elles sont déjà parues sous cette forme.
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
            Version: "2.7.0",
            TitleFr: "Le chat en couleurs, la fiche en onglets",
            TitleEn: "Coloured chat, tabbed profile",
            BodyFr: """
                    **Ajouté**
                    - Le chat met en couleur les conventions d'écriture du RP : emotes entre astérisques ou entre chevrons, hors jeu entre parenthèses, répliques entre guillemets. Tout se passe à la réception et sur votre machine : le message envoyé n'est jamais modifié, et les autres voient exactement ce que vous avez tapé.
                    - Le chat peut afficher le **nom RP** d'un personnage à la place du sien quand sa fiche est visible. Éteint par défaut.
                    - Une commande insère le nom RP de votre cible ou le vôtre dans une réplique, et la copie prête à coller.
                    - Cibler ou survoler un joueur déclaré disponible affiche une **infobulle** avec son nom RP, son état, son statut du moment et son coup d'œil, sans ouvrir sa fiche.
                    - La fiche s'édite en **cinq onglets** au lieu d'une seule page à dérouler. L'ancienne présentation reste disponible dans les réglages.

                    **Modifié**
                    - La fiche reprend le tag **Jeu de rôle** du jeu pour dire si le personnage est en RP, au lieu de vous le faire redire ailleurs. Votre disponibilité n'est publiée que si ce tag est actif : retiré, vous sortez de la liste et des marqueurs, et y revenez dès qu'il est remis.
                    - Les réglages de confidentialité passent de trois à deux, et s'affichent en tête de la page Profil. Une fiche est consultable ou non, en jeu comme par son adresse, et son auteur choisit séparément d'apparaître ou non dans l'annuaire du site.
                    - Les interrupteurs, les préférences et la divinité s'enregistrent d'eux-mêmes, une seconde et demie après le dernier clic. Un texte en cours de saisie attend toujours le bouton.
                    - Le coup d'œil s'édite emplacement par emplacement : les détails s'ajoutent et se retirent un à un au lieu d'occuper cinq blocs vides, et le menu des icônes montre le dessin à côté de son nom.
                    - Le plugin interroge le site par un relevé unique au lieu de trois, et espace ses appels quand sa fenêtre est fermée.

                    **Sécurité**
                    - Une fiche marquée **contenu sensible** ne s'ouvre plus que si votre compte du site accepte ce type de contenu. Le réglage du plugin ne couvre désormais que l'infobulle : l'écran vous dit quoi faire et vous y emmène.
                    """,
            BodyEn: """
                    **Added**
                    - Chat now colours RP writing conventions: emotes between asterisks or angle brackets, out of character between parentheses, spoken lines between quotes. It all happens on reception and on your machine: the message you send is never altered, and others see exactly what you typed.
                    - Chat can show a character's **RP name** instead of their own when their profile is visible. Off by default.
                    - A command inserts your target's RP name or your own into a line, and copies it ready to paste.
                    - Targeting or hovering a player who declared themselves available shows a **tooltip** with their RP name, state, current status and glance, without opening their profile.
                    - The profile is edited in **five tabs** instead of one long page to scroll. The former layout is still available in the settings.

                    **Changed**
                    - The profile now reads the game's **Role-playing** tag to tell whether the character is in character, instead of asking you to say it twice. Your availability is only published while that tag is on: turn it off and you leave the list and the markers, turn it back on and you return.
                    - Privacy settings go from three to two, and sit at the top of the Profile page. A profile is viewable or not, in game as through its address, and its author separately chooses whether to appear in the site directory.
                    - Toggles, preferences and deity save themselves, a second and a half after the last click. Text being typed still waits for the button.
                    - The glance is edited slot by slot: details are added and removed one at a time instead of taking up five empty blocks, and the icon menu shows the drawing next to its name.
                    - The plugin queries the website with a single poll instead of three, and spaces out its calls when its window is closed.

                    **Security**
                    - A profile flagged as **sensitive content** only opens if your website account accepts this kind of content. The plugin setting now only covers the tooltip: the screen tells you what to do and takes you there.
                    """),

        new(
            Version: "2.6.2",
            TitleFr: "Codes de sync",
            TitleEn: "Sync codes",
            BodyFr: """
                    **Ajouté**
                    - Les codes de sync (**Snowcloak**, **Umbra** et consorts) arrivent sur la fiche RP, avec le choix de qui peut les voir.
                    - Un clic copie le code depuis la fiche d'un autre joueur.
                    """,
            BodyEn: """
                    **Added**
                    - Sync codes (**Snowcloak**, **Umbra** and others) are now on the RP profile, with a choice of who can see them.
                    - One click copies the code from another player's profile.
                    """),

        new(
            Version: "2.6.0",
            TitleFr: "Les fiches RP prennent des couleurs",
            TitleEn: "RP profiles get their colours",
            BodyFr: """
                    **Ajouté**
                    - La **bannière**, la **couleur d'accent** et le **portrait** réglés sur le site s'affichent enfin sur la fiche.
                    - Les **adhérents** débloquent un cadre de portrait, un titre personnalisé sous le nom et son animation.
                    - L'**équipe du site** peut afficher son badge, désactivé par défaut.
                    - La fiche se remet à jour d'elle-même après une modification faite sur le site. Un rafraîchissement manuel reste possible.
                    """,
            BodyEn: """
                    **Added**
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
