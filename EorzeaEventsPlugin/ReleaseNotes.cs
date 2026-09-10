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
/// ─── Comment se rédige une note ───────────────────────────────────────────
///
/// Le corps est une liste à puces sans rubrique en en-tête : chaque ligne porte
/// son étiquette, <c>**Nouveauté :**</c>, <c>**Amélioration :**</c>,
/// <c>**Correction :**</c>, <c>**Sécurité :**</c> ou <c>**Merci :**</c>
/// (<c>New</c>, <c>Improvement</c>, <c>Fix</c>, <c>Security</c>, <c>Thanks</c>).
/// <see cref="Ui.Components.MarkdownView"/> les reconnaît et remplace la puce
/// par l'icône colorée correspondante. Jamais d'emoji dans le texte : les
/// polices du jeu n'en ont pas, un carré vide s'afficherait à la place.
///
/// Les règles d'écriture, dans l'ordre où elles servent :
///
/// 1. Décrire le changement concret, du point de vue de ce que le joueur voit,
///    peut faire ou constate dans l'interface. Ni l'implémentation, ni
///    l'intention du développeur.
/// 2. Une conséquence ne s'ajoute que si elle apporte une information utile et
///    non évidente, garantie par la fonctionnalité. Une puce peut parfaitement
///    s'arrêter à la description du changement.
/// 3. Bannir les formulations vagues laissées sans précision : « passe devant »,
///    « là où le jeu met », « facilite l'utilisation ».
/// 4. Chaque pronom, chaque « à la place », chaque « désormais » doit avoir un
///    référent parfaitement clair pour qui découvre la fonctionnalité.
/// 5. Pas de masculin générique : passer par le verbe (« les rôlistes que vous
///    croisez ») ou interroger plutôt que désigner (« qui est à portée de
///    vue »). Les libellés de l'interface gardent leur forme (« Amis RP »).
///
/// Le test avant de valider une ligne : en la retirant, qu'est-ce que le joueur
/// ignore ? Une énumération d'attributs ne répond pas.
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
            Version: "2.10.1",
            TitleFr: "Corrections mineures",
            TitleEn: "Minor fixes",
            BodyFr: """
                    - **Amélioration :** Des corrections mineures et des optimisations ont été apportées au plugin.
                    """,
            BodyEn: """
                    - **Improvement:** Minor fixes and optimisations have been made to the plugin.
                    """),
        new(
            Version: "2.10.0",
            TitleFr: "Votre fiche se tient en jeu",
            TitleEn: "Your profile lives in game",
            BodyFr: """
                    - **Nouveauté :** Un personnage peut avoir jusqu'à trois fiches RP. Une seule est publique à la fois, celle que vous choisissez.
                    - **Nouveauté :** Les textes longs de la fiche s'écrivent dans une fenêtre dédiée, avec des boutons de mise en forme et un aperçu à droite.
                    - **Nouveauté :** Un onglet « Rencontres » garde la liste des personnages croisés en jeu, avec une note libre par personnage. Cette mémoire reste sur votre PC et n'est jamais envoyée au site.
                    - **Nouveauté :** Vous pouvez partager votre position pendant que vous êtes disponible pour du RP. Les personnes équipées du plugin peuvent alors placer un drapeau sur la carte à votre position. Celle-ci est arrondie et n'apparaît jamais sur le site.
                    - **Amélioration :** Le nom RP s'affiche sur la plaque du personnage dans la couleur choisie sur la fiche. Cette couleur ne s'applique jamais au nom réel d'un personnage.
                    - **Amélioration :** Le titre RP remplace désormais le titre habituel sur la plaque du personnage. Quand la disponibilité pour du RP est déclarée, « Dispo RP » s'affiche à la place du titre RP.
                    - **Amélioration :** Un glyphe s'affiche devant le nom de vos amis RP sur leur plaque de personnage.
                    - **Amélioration :** « Autour de moi » classe désormais les joueuses et joueurs selon leur distance : à portée de vue, dans la région ou ailleurs dans le monde.
                    - **Amélioration :** Le plugin utilise désormais une seule entrée dans la barre de statut. Un clic gauche ouvre la fenêtre, un clic droit change votre disponibilité.
                    - **Correction :** « Prolonger (+1h) » ajoute désormais une heure au temps restant de la session, au lieu de ramener sa durée restante à une heure.
                    """,
            BodyEn: """
                    - **New:** A character can have up to three RP profiles. Only one is public at a time, the one you choose.
                    - **New:** Long profile texts are written in a dedicated window, with formatting buttons and a preview on the right.
                    - **New:** An "Encounters" tab keeps the list of characters met in game, with a free-form note for each one. This memory stays on your PC and is never sent to the website.
                    - **New:** You can share your position while you are available for RP. Players with the plugin can then place a flag on the map at your position. That position is rounded, and never appears on the website.
                    - **Improvement:** The RP name appears on the character's nameplate in the colour chosen on the profile. That colour is never applied to a character's real name.
                    - **Improvement:** The RP title now replaces the usual title on the character's nameplate. When availability for RP is declared, "RP Avail" is shown instead of the RP title.
                    - **Improvement:** A glyph appears before the name of your RP friends on their nameplate.
                    - **Improvement:** "Around me" now sorts players by distance: in sight, in this region, or elsewhere in the world.
                    - **Improvement:** The plugin now uses a single entry in the server info bar. Left click opens the window, right click changes your availability.
                    - **Fix:** "Extend (+1h)" now adds an hour to the session's remaining time, instead of resetting that time to one hour.
                    """),
        new(
            Version: "2.9.0",
            TitleFr: "Chacun sous son nom RP",
            TitleEn: "Everyone under their RP name",
            BodyFr: """
                    - **Nouveauté :** Le nom RP remplace le nom du personnage sur sa plaque, dès lors que sa fiche est publique et son tag « Jeu de rôle » allumé. Le ciblage et le clic droit visent toujours le personnage réel, dont le nom reste lisible dans l'infobulle et dans « Autour de moi ».
                    - **Amélioration :** Les noms RP s'affichent dans le chat sans réglage à activer au préalable.
                    - **Amélioration :** Le nom RP sur les plaques et le nom RP dans le chat se coupent séparément, dans les onglets « Rôleplay » et « Discussion » des réglages.
                    """,
            BodyEn: """
                    - **New:** The RP name replaces the character name on their nameplate, as soon as their profile is public and their "Role-playing" tag is on. Targeting and right-click still reach the real character, whose name stays readable in the tooltip and in "Around me".
                    - **Improvement:** RP names appear in chat with no setting to switch on first.
                    - **Improvement:** RP names on nameplates and RP names in chat are turned off separately, in the "Roleplay" and "Chat" settings tabs.
                    """),
        new(
            Version: "2.8.1",
            TitleFr: "La disponibilité RP dit enfin la même chose partout",
            TitleEn: "RP availability now says the same thing everywhere",
            BodyFr: """
                    - **Correction :** « Dispo RP » ne s'affiche plus au-dessus des personnages dont seul le tag « Jeu de rôle » est allumé. Le titre revient aux personnes qui se sont déclarées disponibles ; les autres restent visibles dans « Autour de moi ».
                    - **Correction :** La barre de statut reflète une disponibilité déclarée depuis le site.
                    - **Correction :** La question « rester disponible ? » est de nouveau posée à la connexion quand le réglage est actif.
                    - **Correction :** Une session de RP ouvert ne perd plus son lieu après une déconnexion.
                    """,
            BodyEn: """
                    - **Fix:** "RP Avail" no longer appears above characters who only have the game's "Role-playing" tag on. The title goes back to players who declared themselves available; the others remain visible in "Around me".
                    - **Fix:** The server info bar reflects an availability declared from the website.
                    - **Fix:** The "stay available?" prompt is asked again at login when the setting is on.
                    - **Fix:** An open RP session no longer loses its location after a disconnection.
                    """),
        new(
            Version: "2.8.0",
            TitleFr: "Les réglages en onglets, l'infobulle sous Ctrl",
            TitleEn: "Tabbed settings, tooltip on Ctrl",
            BodyFr: """
                    - **Amélioration :** L'infobulle des joueurs disponibles ne s'affiche plus qu'en maintenant **Ctrl**. La touche se change, ou se retire, dans les réglages.
                    - **Amélioration :** Les réglages sont rangés en cinq onglets : Personnages, Discussion, Rôleplay, Notifications et Divers.
                    - **Amélioration :** L'infobulle a sa propre carte de réglages, au lieu d'être rangée au bas du profil RP.
                    - **Correction :** Une couleur de chat choisie à la roue chromatique était oubliée à la fermeture des réglages.
                    - **Merci :** À Fan, qui a signalé l'infobulle envahissante.
                    """,
            BodyEn: """
                    - **Improvement:** The tooltip on available players now only appears while **Ctrl** is held. The key can be changed, or removed, in the settings.
                    - **Improvement:** Settings are split into five tabs: Characters, Chat, Roleplay, Notifications and Misc.
                    - **Improvement:** The tooltip has its own settings card, instead of sitting at the bottom of the RP profile.
                    - **Fix:** A chat colour picked from the colour wheel was forgotten when the settings closed.
                    - **Thanks:** To Fan, who reported the intrusive tooltip.
                    """),
        new(
            Version: "2.7.2",
            TitleFr: "Les couleurs du chat au choix",
            TitleEn: "Chat colours, your way",
            BodyFr: """
                    - **Amélioration :** Les couleurs des emotes, du hors jeu et des répliques se choisissent sur une roue chromatique, et non plus dans une courte liste.
                    - **Amélioration :** Un nom RP s'affiche dans la couleur de la fiche de son porteur. Le réglage de couleur du nom RP disparaît.
                    - **Correction :** La pastille de couleur par défaut affichait une teinte que le chat ne reprenait pas.
                    - **Correction :** Des libellés de boutons étaient coupés.
                    - **Merci :** À Liraphyra, qui a remonté le bug des couleurs.
                    """,
            BodyEn: """
                    - **Improvement:** Emote, out of character and speech colours are picked on a colour wheel instead of a short list.
                    - **Improvement:** An RP name appears in the colour of its owner's profile. The RP name colour setting is gone.
                    - **Fix:** The default colour swatch showed a shade the chat did not use.
                    - **Fix:** Some button labels were cut off.
                    - **Thanks:** To Liraphyra, who reported the colour bug.
                    """),
        new(
            Version: "2.7.1",
            TitleFr: "Le chat en couleurs, la fiche en onglets",
            TitleEn: "Coloured chat, tabbed profile",
            BodyFr: """
                    - **Nouveauté :** Les personnages dont le tag **Jeu de rôle** est actif et la fiche visible apparaissent dans « Autour de moi ».
                    - **Nouveauté :** Cibler ou survoler quelqu'un de la liste affiche son nom RP, son état et son coup d'œil, sans ouvrir sa fiche.
                    - **Nouveauté :** Le chat colore les emotes, le hors jeu et les répliques. Le message envoyé n'est jamais modifié.
                    - **Amélioration :** La disponibilité suit le tag **Jeu de rôle** : tag retiré, le personnage sort de la liste, et y revient dès qu'il est remis.
                    - **Amélioration :** La fiche s'édite en cinq onglets, et ses interrupteurs s'enregistrent d'eux-mêmes.
                    - **Amélioration :** Un réglage de confidentialité en moins : la fiche est visible ou elle ne l'est pas.
                    - **Sécurité :** Une fiche marquée **contenu sensible** ne s'ouvre que si le compte du lecteur accepte ce contenu.
                    """,
            BodyEn: """
                    - **New:** Characters with the **Role-playing** tag on and a visible profile show up in "Around me".
                    - **New:** Targeting or hovering someone from the list shows their RP name, state and glance, without opening their profile.
                    - **New:** Chat colours emotes, out of character text and spoken lines. The message sent is never altered.
                    - **Improvement:** Availability follows the **Role-playing** tag: tag off, the character leaves the list, and returns as soon as it is back on.
                    - **Improvement:** The profile is edited in five tabs, and its toggles save themselves.
                    - **Improvement:** One privacy setting fewer: the profile is viewable or it is not.
                    - **Security:** A profile flagged as **sensitive content** only opens if the reader's account accepts that content.
                    """),
        new(
            Version: "2.6.2",
            TitleFr: "Codes de sync",
            TitleEn: "Sync codes",
            BodyFr: """
                    - **Nouveauté :** Les codes de sync (**Snowcloak**, **Umbra** et consorts) arrivent sur la fiche RP, avec le choix de qui peut les voir.
                    - **Nouveauté :** Un clic copie le code depuis la fiche de quelqu'un d'autre.
                    """,
            BodyEn: """
                    - **New:** Sync codes (**Snowcloak**, **Umbra** and others) are now on the RP profile, with a choice of who can see them.
                    - **New:** One click copies the code from someone else's profile.
                    """),
        new(
            Version: "2.6.0",
            TitleFr: "Les fiches RP prennent des couleurs",
            TitleEn: "RP profiles get their colours",
            BodyFr: """
                    - **Nouveauté :** La **bannière**, la **couleur d'accent** et le **portrait** réglés sur le site s'affichent sur la fiche.
                    - **Nouveauté :** L'**adhésion** débloque un cadre de portrait, un titre personnalisé sous le nom et son animation.
                    - **Nouveauté :** L'**équipe du site** peut afficher son badge, désactivé par défaut.
                    - **Amélioration :** La fiche se met à jour d'elle-même après une modification faite sur le site. Un rafraîchissement manuel reste possible.
                    """,
            BodyEn: """
                    - **New:** The **banner**, **accent colour** and **portrait** set on the website now show on the profile.
                    - **New:** **Membership** unlocks a portrait frame, a custom title under the name and its animation.
                    - **New:** The **site team** can display its badge, off by default.
                    - **Improvement:** The profile refreshes itself after an edit made on the website. Manual refresh is still available.
                    """),
        new(
            Version: "2.5.2",
            TitleFr: "Plus de faux « lien expiré » après un téléport",
            TitleEn: "No more false \"link expired\" after a teleport",
            BodyFr: """
                    - **Correction :** Le faux **« lien expiré »** après un téléport a disparu : le jeton survit aux écrans de chargement.
                    - **Correction :** Une révocation n'est signalée qu'après plusieurs refus consécutifs. Un incident réseau passager ne suffit plus.
                    """,
            BodyEn: """
                    - **Fix:** The false **"link expired"** after a teleport is gone: the token survives loading screens.
                    - **Fix:** A revocation is only reported after several consecutive rejections. A passing network hiccup no longer counts.
                    """),
        new(
            Version: "2.5.1",
            TitleFr: "L'historique des nouveautés, rattrapé",
            TitleEn: "The what's new history, caught up",
            BodyFr: """
                    - **Correction :** Les versions 2.3.1 à 2.5.0 étaient sorties **sans notes** : tout ce qui a été manqué est déplié ci-dessous.
                    - **Correction :** La fenêtre s'ouvre dès qu'une version non lue a des notes, même si la version installée n'en a pas.
                    """,
            BodyEn: """
                    - **Fix:** Versions 2.3.1 to 2.5.0 shipped **without notes**: everything that was missed is expanded below.
                    - **Fix:** The window opens as soon as an unread version has notes, even when the installed one has none.
                    """),
        new(
            Version: "2.5.0",
            TitleFr: "Compteur « sur place » partout",
            TitleEn: "\"On site\" count everywhere",
            BodyFr: """
                    - **Amélioration :** Le compteur **sur place** d'un RP ouvert fonctionne dans toutes les zones, et plus seulement en quartier résidentiel.
                    - **Amélioration :** L'**instance publique** entre dans le décompte : deux personnages dans « Thanalan occidental 1 » et « 2 » ne sont plus comptés ensemble.
                    """,
            BodyEn: """
                    - **Improvement:** The **on site** count for an open RP session works in every zone, not only in housing wards.
                    - **Improvement:** The **public instance** counts: two characters in "Western Thanalan 1" and "2" are no longer counted together.
                    """),
        new(
            Version: "2.4.1",
            TitleFr: "Une panne ne ressemble plus à une liste vide",
            TitleEn: "An outage no longer looks like an empty list",
            BodyFr: """
                    - **Nouveauté :** Les personnages détectés sur place, l'avertissement de contenu sensible et le numéro d'appartement s'affichent.
                    - **Correction :** Site injoignable ou erreur serveur : les listes **gardent leur contenu** et proposent de réessayer, au lieu de paraître vides.
                    - **Correction :** L'enregistrement d'une fiche dit s'il a réussi, et un champ vidé est réellement effacé.
                    - **Correction :** Douze thèmes et neuf races restaient en français dans la version anglaise.
                    """,
            BodyEn: """
                    - **New:** Characters detected on site, the sensitive content warning and the apartment number now appear.
                    - **Fix:** Site unreachable or server error: lists **keep their content** and offer to retry, instead of looking empty.
                    - **Fix:** Saving a profile reports whether it worked, and a cleared field is really erased.
                    - **Fix:** Twelve themes and nine races stayed in French in the English build.
                    """),
        new(
            Version: "2.4.0",
            TitleFr: "Amis RP et confidentialité",
            TitleEn: "RP friends and privacy",
            BodyFr: """
                    - **Nouveauté :** Nouvel onglet **Amis RP** : les personnages à qui la fiche est ouverte, avec marqueur de réciprocité et note privée.
                    - **Nouveauté :** L'ajout se fait par **clic droit sur un personnage**, sans notification envoyée.
                    - **Nouveauté :** Nouvel échelon de visibilité **« Mes amis RP »**, avec un aperçu de ce qu'un ami voit.
                    - **Correction :** Republier une fiche depuis le jeu n'écrase plus les réglages faits sur le site : page web, indexation et sections masquées restent en place.
                    """,
            BodyEn: """
                    - **New:** New **RP friends** tab: the characters the profile is open to, with a mutual marker and a private note.
                    - **New:** Adding a friend happens by **right-clicking a character**, with no notification sent.
                    - **New:** New **"My RP friends"** visibility tier, with a preview of what a friend sees.
                    - **Fix:** Republishing a profile from the game no longer overwrites settings made on the website: web page, search indexing and hidden sections stay as they were.
                    """),
        new(
            Version: "2.3.2",
            TitleFr: "Accroches et thèmes conservés",
            TitleEn: "Hooks and themes preserved",
            BodyFr: """
                    - **Correction :** L'assistant de première configuration effaçait les **accroches**, les **thèmes recherchés** et les **thèmes évités**. Il ne touche plus qu'à ce qu'il affiche.
                    """,
            BodyEn: """
                    - **Fix:** The first-time setup wizard erased **hooks**, **sought themes** and **avoided themes**. It now only touches what it displays.
                    """),
        new(
            Version: "2.3.1",
            TitleFr: "Fiche préservée et portrait agrandi",
            TitleEn: "Profile preserved, larger portrait",
            BodyFr: """
                    - **Nouveauté :** Portrait plus grand dans la fiche comme dans les listes, et cliquable pour l'afficher en grand.
                    - **Correction :** La **disponibilité RP** n'efface plus ce qui ne s'édite que sur le site : le reste de la fiche est laissé intact.
                    - **Correction :** La disponibilité RP affiche une erreur au lieu de rester allumée à tort quand le serveur refuse.
                    - **Correction :** Plus de seconde barre de défilement dans les fenêtres à pied fixe.
                    """,
            BodyEn: """
                    - **New:** Larger portrait in the profile and in lists, clickable to view it full size.
                    - **Fix:** **RP availability** no longer wipes what can only be edited on the website: the rest of the profile is left intact.
                    - **Fix:** RP availability shows an error instead of wrongly staying on when the server refuses.
                    - **Fix:** No more second scrollbar in windows with a fixed footer.
                    """),
        new(
            Version: "2.3.0",
            TitleFr: "Nouvelle interface, fiches RP et confidentialité",
            TitleEn: "A new interface, RP profiles and privacy",
            BodyFr: """
                    - **Nouveauté :** Interface repensée : navigation par menu latéral, cartes plus lisibles, et textes du site rendus avec leur mise en forme.
                    - **Nouveauté :** Une **fiche RP par personnage**, partagée section par section, avec le référencement par les moteurs désactivé par défaut.
                    - **Nouveauté :** Page **Autour de moi**, et clic droit sur un personnage pour consulter sa fiche.
                    - **Nouveauté :** Événements regroupés par jour, avec recherche, filtres et bouton **Y aller** via Lifestream.
                    - **Correction :** Un RP démarré en quartier résidentiel affiche la bonne zone.
                    - **Correction :** Le plugin ne plante plus au changement de zone.
                    """,
            BodyEn: """
                    - **New:** Redesigned interface: sidebar navigation, clearer cards, and website text rendered with its formatting.
                    - **New:** One **RP profile per character**, shared section by section, with search engine listing off by default.
                    - **New:** New **Around me** page, plus right-click on a character to view their profile.
                    - **New:** Events grouped by day, with search, filters and a **Go there** button via Lifestream.
                    - **Fix:** An RP session started in a housing ward shows the right zone.
                    - **Fix:** The plugin no longer crashes when changing zone.
                    """),
        new(
            Version: "2.2.0",
            TitleFr: "Un jeton par personnage",
            TitleEn: "One token per character",
            BodyFr: """
                    - **Nouveauté :** **Un jeton d'accès par personnage** au lieu d'un seul pour tout le compte, choisi automatiquement selon le personnage connecté.
                    - **Nouveauté :** Plusieurs personnages liés se gèrent depuis les paramètres.
                    - **Nouveauté :** Paliers de fidélité et suivi de présence dans les établissements.
                    - **Correction :** Les événements annulés disparaissaient de la liste sans explication.
                    """,
            BodyEn: """
                    - **New:** **One access token per character** instead of a single account-wide one, picked automatically for the character in play.
                    - **New:** Several linked characters can be managed from the settings.
                    - **New:** Loyalty tiers and presence tracking in venues.
                    - **Fix:** Cancelled events vanished from the list without explanation.
                    """),
        new(
            Version: "2.1.0",
            TitleFr: "Disponibilité RP et première fiche",
            TitleEn: "RP availability and the first profile",
            BodyFr: """
                    - **Nouveauté :** La **disponibilité RP** devient un statut permanent plutôt qu'une annonce ponctuelle.
                    - **Nouveauté :** Première version de la **fiche RP** : niveau de jeu, mode d'approche, langues.
                    - **Nouveauté :** Les informations de logement accompagnent la présence, de quoi se faire retrouver dans un quartier résidentiel.
                    """,
            BodyEn: """
                    - **New:** **RP availability** becomes a lasting status rather than a one-off announcement.
                    - **New:** First version of the **RP profile**: play level, approach style, languages.
                    - **New:** Housing details travel with presence, making a character findable inside a residential ward.
                    """),
        new(
            Version: "2.0.0",
            TitleFr: "Refonte de la configuration",
            TitleEn: "Setup rework",
            BodyFr: """
                    - **Amélioration :** Fenêtre de configuration et composants d'interface repensés : la liaison du compte est plus claire à la première utilisation.
                    """,
            BodyEn: """
                    - **Improvement:** Reworked setup window and interface components: linking an account is clearer on first use.
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
