using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using EorzeaEventsPlugin.Api;
using System.Linq;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Rendu en lecture seule d'une fiche RP.
///
/// Le même rendu sert à deux écrans : la fiche de son propre personnage
/// (<c>Ui.Pages.RpProfilePage</c>, qui délègue ici ses sections non éditables) et
/// la fiche d'un autre joueur (<c>Windows.RpProfileWindow</c> en mode
/// consultation). Avant cette mise en commun, la seconde se contentait de quatre
/// lignes en ImGui brut alors que les données étaient déjà disponibles.
///
/// Les libellés de vocabulaire restent ici plutôt que dans <see cref="Loc"/>
/// quand ils sont identiques dans les deux langues du plugin : thèmes, races et
/// divinités portent des noms propres.
/// </summary>
internal static class RpProfileView
{
    /// <summary>
    /// Fiche complète, en-tête compris. Sert à consulter le personnage d'un
    /// autre joueur.
    /// </summary>
    public static void Draw(RpProfileDto? profile, string characterName, string? server, Loc l)
    {
        DrawHeader(profile, characterName, server, l);

        if (profile == null)
        {
            Feedback.EmptyState(Icons.Profile, l.RpProfileNoProfile);
            return;
        }

        // Contenu retenu par le site : la fiche est arrivée vide, et c'est voulu.
        // Le consentement se donne sur le compte, pas dans le plugin : un réglage
        // local ne peut pas décider ce que le serveur accepte d'envoyer.
        if (profile.NsfwWithheld)
        {
            Feedback.EmptyState(Icons.Warning, l.RpProfileNsfwWithheld,
                                l.RpProfileNsfwWithheldHint,
                                l.RpProfileNsfwWithheldCta,
                                () => OpenUrl(Plugin.Config.BaseUrl + "/dashboard/profil"));
            return;
        }

        // Une seule fois par fiche : EnsureReadable coûte un calcul de luminance,
        // et surtout toutes les sections doivent porter exactement la même teinte.
        var tone = Accent(profile);

        DrawHooks(profile, l, tone);
        DrawGlances(profile, l, tone);
        DrawPreferences(profile, l, tone);
        DrawIdentity(profile, l, tone);
        DrawTraits(profile, l, tone);
        DrawBelonging(profile, l, tone);
        DrawSyncshells(profile, l, tone);
        DrawRelations(profile, l, tone);
        DrawStory(profile, l, tone);
        DrawLinks(profile, l, tone);
    }

    /// <summary>
    /// Couleur d'accent de la fiche, toujours lisible sur le thème sombre.
    ///
    /// La couleur est saisie sur le site, sur un fond clair : une teinte très
    /// sombre y passe pour élégante et devient invisible ici. EnsureReadable la
    /// remonte au besoin. Une valeur absente ou malformée retombe sur l'accent
    /// du thème, ce qui reproduit exactement le rendu d'avant.
    /// </summary>
    public static Vector4 Accent(RpProfileDto? profile) =>
        Theme.EnsureReadable(Theme.TryParseHex(profile?.AccentColor) ?? Theme.Accent);

    /// <summary>
    /// Seconde couleur du dégradé, réservée aux membres. Symétrique de
    /// <see cref="Accent"/>, EnsureReadable compris.
    ///
    /// Elle retombe sur la première et non sur l'accent du thème : toute moyenne
    /// des deux vaut alors exactement la première, ce qui rend le rendu d'une
    /// fiche à une seule couleur strictement identique à celui d'avant.
    /// </summary>
    public static Vector4 Accent2(RpProfileDto? profile) =>
        Theme.EnsureReadable(Theme.TryParseHex(profile?.AccentColor2)
                             ?? Theme.TryParseHex(profile?.AccentColor)
                             ?? Theme.Accent);

    /// <summary>
    /// Teinte de fond de l'entête, moyenne des deux couleurs de la fiche.
    ///
    /// Un vrai dégradé bicolore demanderait un AddRectFilledMultiColor sur la
    /// carte, hors de l'API de <see cref="Card.Begin"/>. Le facteur 0,12 reste le
    /// plafond, appliqué à la moyenne.
    /// </summary>
    internal static Vector4 HeaderBackground(Vector4 accent, Vector4 accent2) =>
        Theme.Mix(Theme.BgSurface, Theme.Mix(accent, accent2, 0.5f), 0.12f);

    /// <summary>
    /// Libellé du statut d'équipe, ou null s'il n'y a rien à afficher.
    ///
    /// Un rôle inconnu ne donne pas de pastille, plutôt qu'une pastille au
    /// libellé brut : c'est un signe d'autorité, il ne doit rien afficher qu'on
    /// ne sache nommer.
    ///
    /// <paramref name="requireConsent"/> porte une asymétrie voulue entre les
    /// deux vues, à NE PAS unifier :
    ///
    /// • Vue publique (aperçu, fiche d'autrui, « Autour de moi ») : faux. Ces
    ///   payloads viennent de la sérialisation publique, qui n'expose PAS
    ///   `staffBadgeVisible` (exprès : masquer son rôle et ne pas en avoir doivent
    ///   y être indiscernables, sinon la route anonyme devient un annuaire du
    ///   staff) et qui met déjà `staffRole` à null quand le consentement manque.
    ///   Le champ absent du JSON valant false en C#, exiger le consentement ici
    ///   n'affichait le badge de personne, jamais.
    ///
    /// • Vue propriétaire (page d'édition) : vrai. Ce payload-là renvoie
    ///   `staffRole` même sans consentement, faute de quoi le plugin ne pourrait
    ///   pas proposer la case qui l'active. C'est donc au client de respecter la
    ///   case, sinon le badge s'afficherait dans la page d'édition alors qu'elle
    ///   est décochée.
    /// </summary>
    public static string? StaffBadgeLabel(RpProfileDto? profile, Loc l, bool requireConsent)
    {
        if (profile == null) return null;
        if (requireConsent && !profile.StaffBadgeVisible) return null;

        return profile.StaffRole switch
        {
            "ADMIN"     => l.RpProfileStaffAdmin,
            "MODERATOR" => l.RpProfileStaffModerator,
            _           => null,
        };
    }

    /// <summary>
    /// Pastille de statut d'équipe, dans une couleur de thème FIXE.
    ///
    /// Jamais l'accent de la fiche : n'importe qui peut choisir sa couleur sur le
    /// site, et une pastille teintée par son porteur permettrait de maquiller sa
    /// fiche pour imiter le vrai badge.
    /// </summary>
    /// <param name="requireConsent">
    /// Voir <see cref="StaffBadgeLabel"/> : faux pour toute vue alimentée par la
    /// sérialisation publique, vrai pour la page d'édition du propriétaire.
    /// </param>
    public static bool StaffBadge(RpProfileDto? profile, Loc l, bool requireConsent)
    {
        if (StaffBadgeLabel(profile, l, requireConsent) is not { } label) return false;

        // Le libellé nomme le site, et l'infobulle le redit en toutes lettres,
        // comme sur la page web : une pastille dorée marquée « Administration »
        // au-dessus d'un nom se confondrait avec un rang de compagnie libre ou un
        // rôle Discord. Nommer l'autorité fait partie du badge, au même titre que
        // sa couleur fixe. L'infobulle ne rend pas la pastille interactive :
        // Chip.Draw pose un Dummy, le survol suffit.
        Chip.Draw(label, ChipTone.Gold, Icons.Shield, tooltip: l.RpProfileStaffTitle);
        return true;
    }

    // ─── En-tête ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Dimensions de l'entête, partagées avec RpProfilePage : les deux écrans
    /// affichent la même fiche et doivent chevaucher de la même façon.
    /// </summary>
    internal const float HeaderPortrait = 200f;
    internal const float HeaderBanner   = 160f;

    /// <summary>
    /// Décalage du portrait sur la bannière. Nul sans bannière : il n'y aurait
    /// rien à chevaucher et le portrait mordrait sur le bord de la carte.
    /// </summary>
    /// <summary>
    /// Écart entre la pastille de statut d'équipe et le nom qu'elle surmonte.
    /// Partagé entre le dessin et le calcul de hauteur du bloc de nom : deux
    /// valeurs distinctes décaleraient le centrage vertical.
    /// </summary>
    private const float HeaderBadgeGap = Theme.GapXs;

    internal static float HeaderOverlap(bool hasBanner) =>
        hasBanner ? HeaderPortrait * 0.5f : 0f;

    /// <summary>
    /// Centre verticalement le bloc de nom sur la hauteur du portrait, en
    /// poussant devant lui la moitié du vide disponible.
    ///
    /// Le bloc était auparavant calé en bas du portrait, avec une hauteur estimée
    /// à 2,4 interlignes. En jeu, la bannière étant chevauchée de la demi-hauteur
    /// du portrait, tout ce vide s'accumulait entre le bas de la bannière et le
    /// nom : une centaine de pixels de trou à droite du portrait.
    ///
    /// Le centrage ne pardonne pas une hauteur approximative : un écart de
    /// quelques pixels se voit immédiatement, puisqu'il se partage entre le haut
    /// et le bas. La hauteur est donc mesurée sur ce qui sera réellement dessiné,
    /// dans les polices qui serviront à le dessiner, y compris la pastille de
    /// titre dont la taille dépend de la place disponible. Les paramètres portent
    /// exactement les chaînes que l'appelant s'apprête à afficher : les deviner
    /// ici rouvrirait l'écart entre la mesure et le rendu.
    ///
    /// Appelée par les deux écrans qui affichent la fiche, qui doivent chevaucher
    /// et cadrer de la même façon.
    /// </summary>
    /// <param name="badge">
    /// Libellé du statut d'équipe, dessiné en pastille au-dessus du nom. Nul, il
    /// ne compte pas : un profil sans rôle ne doit pas gagner de vide.
    /// </param>
    /// <param name="name">Nom affiché en gros, rendu par <see cref="Text.Title"/>.</param>
    /// <param name="title">Titre RP, rendu en pastille. Nul, il ne compte pas.</param>
    /// <param name="identity">Ligne « personnage · serveur », absente sur sa propre fiche.</param>
    /// <param name="nickname">Surnom entre guillemets, facultatif.</param>
    /// <param name="minTop">
    /// Ordonnée écran sous laquelle le bloc doit commencer, ou 0 pour ne rien
    /// borner. Le portrait remonte de sa demi-hauteur sur la bannière, et le
    /// centrage part donc du haut du portrait : sans borne, le vide de centrage
    /// laisse le premier élément dessiné, pastille d'équipe ou nom, au-dessus du
    /// bord bas de la bannière, et le nom se lit par-dessus l'image.
    ///
    /// C'est bien le vide qu'on borne, et pas le chevauchement du portrait :
    /// celui-ci est calé sur le rendu du site et les deux surfaces doivent rester
    /// identiques. Le centrage reste donc la règle, il ne peut simplement plus
    /// faire déborder le bloc vers le haut.
    /// </param>
    internal static void HeaderNameFiller(string? badge, string name, string? title,
                                          string? identity, string? nickname,
                                          float minTop = 0f)
    {
        var block  = HeaderNameHeight(badge, name, title, identity, nickname);
        var filler = (Theme.S(HeaderPortrait) - block) * 0.5f;

        if (minTop > 0f)
            filler = MathF.Max(filler, minTop - ImGui.GetCursorScreenPos().Y);

        if (filler > 0f) ImGui.Dummy(new Vector2(0f, filler));
    }

    /// <summary>
    /// Ordonnée écran à partir de laquelle le bloc de nom peut être dessiné :
    /// le bas de la bannière, plus une respiration.
    ///
    /// Mesurée depuis l'origine de la carte plutôt que déduite des rembourrages
    /// internes de <see cref="Card.Begin"/> : ceux-ci peuvent bouger, la hauteur
    /// de bannière que l'on vient de demander, non.
    /// </summary>
    internal static float HeaderNameMinTop(Vector2 cardOrigin, bool hasBanner) =>
        hasBanner ? cardOrigin.Y + Theme.S(HeaderBanner) + Theme.S(Theme.GapS) : 0f;

    /// <summary>
    /// Hauteur réelle du bloc de nom, interlignes compris.
    ///
    /// Chaque ligne est mesurée dans sa propre police : la police courante
    /// détermine CalcTextSize, et le nom est écrit dans la police de titre, plus
    /// haute qu'une ligne ordinaire. La largeur de repli est celle que les
    /// appelants viennent de pousser, sans quoi un nom long compterait pour une
    /// ligne alors qu'il en occupera deux.
    /// </summary>
    private static float HeaderNameHeight(string? badge, string name, string? title,
                                          string? identity, string? nickname)
    {
        var wrap  = MathF.Max(ImGui.GetContentRegionAvail().X - Card.RightInset, Theme.S(40f));
        var block = 0f;
        var lines = 0;

        // Badge d'équipe et son écart : deux éléments, donc deux interlignes à
        // compter. Sans ce terme, le centrage serait faux pour les seuls comptes
        // d'équipe, c'est-à-dire là où personne ne pense à le vérifier.
        if (badge is { Length: > 0 })
        {
            block += Chip.Height() + Theme.S(HeaderBadgeGap);
            lines += 2;
        }

        using (Fonts.PushTitle())
        {
            block += ImGui.CalcTextSize(Glyphs.Safe(name), false, wrap).Y;
            lines++;
        }

        // La pastille connaît seule sa police et son rembourrage : lui demander
        // sa taille est le seul moyen de rester d'accord avec ce qu'elle dessine.
        var pill = AnimatedText.Measure(title);
        if (pill.Y > 0f)
        {
            block += pill.Y;
            lines++;
        }

        // Même police que le dessin ci-dessus. Mesurer en 12 px ce qui est
        // dessiné en 15 fausserait le centrage de plusieurs pixels par ligne.
        using (Fonts.PushBody())
        {
            if (!string.IsNullOrWhiteSpace(identity))
            {
                block += ImGui.CalcTextSize(Glyphs.Safe(identity), false, wrap).Y;
                lines++;
            }

            if (!string.IsNullOrWhiteSpace(nickname))
            {
                block += ImGui.CalcTextSize(Glyphs.Safe(nickname), false, wrap).Y;
                lines++;
            }
        }

        // ImGui insère son interligne entre deux éléments, donc une fois de moins
        // qu'il n'y a de lignes.
        return block + ImGui.GetStyle().ItemSpacing.Y * MathF.Max(lines - 1, 0);
    }

    private static void DrawHeader(RpProfileDto? profile, string characterName, string? server, Loc l)
    {
        // Habillage choisi par l'auteur sur le site. EnsureReadable n'est pas
        // optionnel : la couleur est choisie sur un fond clair et peut être
        // quasi noire, ce qui rendrait la citation invisible ici.
        var accent    = Accent(profile);
        var accent2   = Accent2(profile);
        var hasAccent = Theme.TryParseHex(profile?.AccentColor) != null;
        var banner    = Textures.Get(profile?.BannerUrl);

        // Un effet de cadre sans couleur choisie mérite quand même son liseré :
        // sinon l'habillage payé sur le site ne se voit nulle part en jeu.
        var hasFrame = hasAccent || profile?.FrameStyle is { Length: > 0 };

        // Relevée avant la carte : c'est le seul moment où le curseur est encore
        // sur son coin haut gauche, d'où se déduit le bas de la bannière.
        var cardOrigin = ImGui.GetCursorScreenPos();

        using var card = Card.Begin("rpview_header", interactive: false,
            background:   hasAccent ? HeaderBackground(accent, accent2) : null,
            accent:       hasAccent ? accent : null,
            banner:       banner,
            bannerHeight: HeaderBanner);

        // Le portrait remonte sur la bannière, à la manière d'un profil Discord.
        var overlap = HeaderOverlap(banner != null);
        if (overlap > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - Theme.S(overlap));

        DrawPortrait(profile?.PortraitUrl, characterName,
                     height:     HeaderPortrait,
                     frame:      hasFrame ? accent : null,
                     frameStyle: profile?.FrameStyle,
                     frame2:     accent2);

        ImGui.SameLine(0f, Theme.S(Theme.GapM));

        ImGui.BeginGroup();

        // Le portrait mange une bonne part de la largeur : sans repli, un nom RP
        // un peu long sort de la carte, ces textes ne bouclant pas d'eux-mêmes.
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X
                              - Card.RightInset);

        // Les chaînes sont composées avant d'être mesurées puis dessinées : le
        // centrage vertical se fait sur ce qui sera affiché, pas sur une
        // approximation reconstituée deux fois.
        var displayName = profile?.RpName is { Length: > 0 } rpName ? rpName : characterName;

        // Le nom du personnage ne se répète pas sous lui-même. Le cas le plus
        // fréquent est de jouer sous son nom de personnage : la ligne affichait
        // alors le même nom en petit juste sous le même nom en gros. Quand les
        // deux diffèrent, la ligne complète reste indispensable, c'est elle qui dit
        // qui joue derrière le nom RP.
        //
        // La chaîne est composée une seule fois, puis mesurée et dessinée : la
        // mesure sert au centrage vertical du bloc, une chaîne reconstituée
        // autrement décalerait tout l'entête dans ce cas pourtant courant.
        var identity = string.IsNullOrWhiteSpace(server)
            ? null
            : string.Equals(displayName.Trim(), characterName.Trim(),
                            StringComparison.OrdinalIgnoreCase)
                ? server
                : $"{characterName} · {server}";

        var nickname    = profile?.Nickname is { Length: > 0 } nick ? $"« {nick} »" : null;
        // Vue publique : le serveur a déjà appliqué le consentement.
        var badge       = StaffBadgeLabel(profile, l, requireConsent: false);

        HeaderNameFiller(badge, displayName, profile?.RpTitle, identity, nickname,
                         HeaderNameMinTop(cardOrigin, banner != null));

        // Statut d'équipe au-dessus du nom : c'est l'information qui change la
        // façon dont on lit tout le reste de la fiche, elle doit être vue avant
        // le nom et non reléguée parmi les chips de synthèse.
        if (StaffBadge(profile, l, requireConsent: false))
            Layout.Spacer(HeaderBadgeGap);

        Text.Title(displayName);

        // Titre court réservé aux membres, juste sous le nom : c'est là qu'un
        // titre se lit, avant l'identité technique du personnage.
        AnimatedText.Draw(profile?.RpTitle, accent2, profile?.TitleAnimation, accent);

        // Police de corps, et non la petite : ces lignes appartiennent à
        // l'entête, que le reste de la fiche lit en 15 px. En 12 px elles
        // paraissaient reléguées, alors qu'elles portent l'identité du
        // personnage. Le retrait passe par la couleur, pas par la taille.
        if (identity != null) Text.Body(identity, Theme.TextMuted);
        if (nickname != null) Text.Body(nickname, Theme.TextMuted);

        ImGui.PopTextWrapPos();
        ImGui.EndGroup();

        DrawHeaderFooter(profile, accent, l);
    }

    /// <summary>
    /// Bande basse de l'entête : citation, disponibilité et chips de synthèse.
    ///
    /// Le niveau et les langues figurent aussi dans la carte Préférences. La
    /// répétition est assumée : ce sont les deux critères sur lesquels on décide
    /// d'aborder quelqu'un, ils doivent se lire sans faire défiler. Même parti
    /// pris que la liste « Autour de moi ».
    /// </summary>
    private static void DrawHeaderFooter(RpProfileDto? profile, Vector4 accent, Loc l)
    {
        if (profile == null) return;

        Layout.Spacer(Theme.GapS);

        // Instant présent, avant tout le reste de la bande basse : c'est la seule
        // information de la fiche qui se périme dans la soirée, et celle sur
        // laquelle se décide si l'on aborde quelqu'un maintenant. La reléguer
        // sous la citation la ferait lire après ce qui ne change jamais.
        var state     = profile.IcState   is { Length: > 0 } ? profile.IcState   : null;
        var currently = profile.Currently is { Length: > 0 } ? profile.Currently : null;

        if (state != null)
            Chip.Draw(IcStateLabel(state, l), IcStateTone(state), Icons.RpLive);

        if (currently != null)
        {
            if (state != null) Layout.Spacer(Theme.GapXs);
            Text.Body(currently);
        }

        if (state != null || currently != null) Layout.Spacer(Theme.GapXs);

        if (profile.Quote is { Length: > 0 } quote)
        {
            Text.Body($"« {quote} »", accent);
            Layout.Spacer(Theme.GapXs);
        }

        // La disponibilité porte son étiquette, comme sur le site. Sans elle,
        // « Le soir et les weekends » s'affichait sous la citation sans que rien
        // ne dise de quoi il s'agissait, et se lisait comme la suite du texte
        // libre du joueur.
        if (profile.Availability is { Length: > 0 } availability)
        {
            Text.Body($"{l.RpProfileAvailabilityLabel} : {availability}", Theme.TextMuted);
            Layout.Spacer(Theme.GapXs);
        }

        // Le statut d'équipe est remonté dans le bloc de nom : la ligne de chips
        // s'ouvre donc sur le niveau, qui n'a plus de SameLine à recevoir.
        Chip.Colored(LevelLabel(profile.RpLevel, l), accent);

        if (profile.Languages.Length > 0)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(string.Join(" / ", profile.Languages.Select(LanguageLabel)),
                      ChipTone.Neutral);
        }

        if (profile.Nsfw)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(l.RpProfileNsfw, ChipTone.Danger, Icons.Warning);
        }
    }

    /// <summary>
    /// Portrait téléversé depuis le site, cadré en 3:4 et recadré en mode
    /// « couvrir » : la source fait 480×640, tout autre ratio serait déformé.
    ///
    /// Tant que l'image n'est pas arrivée, ou s'il n'y en a pas, un cadre aux
    /// initiales tient la place. Il réserve exactement les mêmes dimensions, sans
    /// quoi la carte grandit quand la texture arrive : le cercle de
    /// <see cref="Layout.Avatar"/> ne réservait qu'un carré, soit un quart de
    /// hauteur en moins.
    ///
    /// Un clic ouvre le portrait en grand, seul moyen de le voir à sa résolution
    /// réelle sans passer par le site.
    /// </summary>
    /// <param name="frame">
    /// Couleur d'un cadre de 2 px autour du portrait. Sert à faire porter la
    /// couleur de la fiche par le portrait quand il chevauche la bannière.
    /// Null, aucun cadre n'est dessiné.
    /// </param>
    /// <param name="frameStyle">
    /// Effet de cadre réservé aux membres, servi par le serveur. Nul ou inconnu,
    /// le cadre est celui d'origine : voir <see cref="PortraitFrame"/>.
    /// </param>
    /// <param name="frame2">
    /// Seconde couleur de la fiche, dont seul le cadre bicolore se sert. Absente,
    /// le cadre retombe sur <paramref name="frame"/> : les listes qui n'ont pas
    /// de fiche complète sous la main n'ont donc rien à passer.
    /// </param>
    public static void DrawPortrait(string? portraitUrl, string characterName,
                                    float height = 200f, Vector4? status = null,
                                    string? id = null, bool zoomable = true,
                                    Vector4? frame = null, string? frameStyle = null,
                                    Vector4? frame2 = null)
    {
        var width  = height * 3f / 4f;
        var size   = new Vector2(Theme.S(width), Theme.S(height));
        var origin = ImGui.GetCursorScreenPos();
        var dl     = ImGui.GetWindowDrawList();
        var radius = Theme.S(Theme.RadiusCard);

        var texture = Textures.Get(portraitUrl);

        if (texture != null)
        {
            var (uv0, uv1) = Surface.CoverUv(texture.Width, texture.Height, size.X, size.Y);
            dl.AddImageRounded(texture.Handle, origin, origin + size, uv0, uv1,
                               ImGui.GetColorU32(Vector4.One), radius);
        }
        else
        {
            DrawInitialsFrame(dl, origin, size, characterName, radius);
        }

        // Après l'image : le cadre doit border le portrait, pas passer dessous.
        if (frame is { } frameColor)
            PortraitFrame.Draw(dl, origin, origin + size, radius, frameColor, frameStyle, frame2);

        // Un bouton invisible plutôt qu'un Dummy : mêmes dimensions réservées,
        // mais le survol et le clic deviennent détectables. Le retour vaut au
        // relâchement, ce qui évite d'ouvrir l'agrandissement en déplaçant la
        // fenêtre depuis le portrait.
        var key     = id is { Length: > 0 } ? id : characterName;
        var clicked = ImGui.InvisibleButton($"##portrait_{key}", size);

        var interactive = zoomable && texture != null;
        if (interactive && ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            dl.AddRect(origin, origin + size, ImGui.GetColorU32(Theme.BorderLight),
                       radius, ImDrawFlags.None, Theme.S(1.5f));
            Feedback.Tooltip(Plugin.L.RpProfileZoom);
        }

        if (status is { } dot) DrawStatusDot(dl, origin, size, dot);

        if (interactive && clicked)
            Plugin.OpenPortraitZoom(portraitUrl!, characterName);
    }

    /// <summary>Cadre 3:4 aux initiales, en attente du portrait ou à défaut.</summary>
    private static void DrawInitialsFrame(ImDrawListPtr dl, Vector2 origin, Vector2 size,
                                          string characterName, float radius)
    {
        var background = Theme.FromName(characterName);
        dl.AddRectFilled(origin, origin + size, ImGui.GetColorU32(background), radius);

        var initials = Layout.Initials(characterName);
        // Sur un grand cadre, deux lettres en petite police se perdent au milieu.
        using var font = size.X >= Theme.S(96f) ? Fonts.PushTitle() : Fonts.PushSmall();
        var textSize = ImGui.CalcTextSize(initials);
        dl.AddText(origin + (size - textSize) * 0.5f,
                   ImGui.GetColorU32(Theme.TextOn(background)), initials);
    }

    /// <summary>
    /// Pastille de présence dans l'angle du portrait. Dessinée dans les deux cas :
    /// tant qu'elle ne l'était que sur le cadre d'initiales, le point « en ligne »
    /// disparaissait dès que l'image finissait de charger.
    /// </summary>
    private static void DrawStatusDot(ImDrawListPtr dl, Vector2 origin, Vector2 size,
                                      Vector4 color)
    {
        var radius = MathF.Max(Theme.S(4f), size.X * 0.09f);
        var inset  = radius + Theme.S(4f);
        var center = new Vector2(origin.X + size.X - inset, origin.Y + size.Y - inset);

        dl.AddCircleFilled(center, radius + Theme.S(1.5f), ImGui.GetColorU32(Theme.BgSurface));
        dl.AddCircleFilled(center, radius, ImGui.GetColorU32(color));
    }

    // ─── Sections ─────────────────────────────────────────────────────────────

    public static void DrawHooks(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        var hooks = p.Hooks.Where(h => !string.IsNullOrWhiteSpace(h)).ToArray();
        if (hooks.Length == 0 && string.IsNullOrWhiteSpace(p.CurrentQuest)) return;

        using var card = Card.Begin("rpview_hooks", interactive: false);
        Layout.SectionHeader(l.RpProfileHooks, Icons.Sparkle, tone: tone);
        BeginRows();

        if (p.CurrentQuest is { Length: > 0 } quest)
        {
            Row(l.RpProfileCurrentQuest, quest);
            if (hooks.Length > 0) Layout.Spacer(Theme.GapXs);
        }

        foreach (var hook in hooks)
            Text.WithIcon(Icons.Chevron, hook, tone ?? Theme.Accent, wrap: true);
    }

    /// <summary>
    /// Coup d'œil : ce que l'on remarque du personnage avant même de lui parler,
    /// une icône et un titre par ligne, la description au survol.
    ///
    /// Le rendu des lignes vit dans <see cref="GlanceRows"/>, à part de la carte :
    /// la même rangée devra tenir dans une infobulle de survol, où il n'y a ni
    /// carte ni en-tête de section.
    ///
    /// Rien à filtrer sur la fiche d'autrui, le serveur ne sert les emplacements
    /// éteints qu'à leur propriétaire ; le filtre reste néanmoins ici, puisque
    /// c'est exactement cette vue qui lui sert d'aperçu.
    /// </summary>
    public static void DrawGlances(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        if (!HasGlances(p)) return;

        using var card = Card.Begin("rpview_glance", interactive: false);
        Layout.SectionHeader(l.RpProfileGlance, Icons.Show, tone: tone);
        GlanceRows(p, tone);
    }

    /// <summary>Au moins un emplacement allumé et titré, donc quelque chose à montrer.</summary>
    public static bool HasGlances(RpProfileDto p) => p.Glances.Any(Visible);

    /// <summary>
    /// Rangée d'icônes titrées, sans cadre : réutilisable partout où le coup
    /// d'œil doit apparaître, carte de fiche comme infobulle.
    /// </summary>
    public static void GlanceRows(RpProfileDto p, Vector4? tone = null)
    {
        foreach (var glance in p.Glances.Where(Visible))
        {
            // Le groupe donne à l'icône et au titre une seule zone de survol :
            // viser l'un ou l'autre doit révéler la même description.
            ImGui.BeginGroup();
            Text.WithIcon(Icons.Glance(glance.Icon), glance.Title,
                          tone ?? Theme.Accent, wrap: true);
            ImGui.EndGroup();

            if (!string.IsNullOrWhiteSpace(glance.Body))
                Feedback.TooltipOnHover(glance.Body, glance.Title);
        }
    }

    /// <summary>
    /// Un emplacement éteint, ou allumé mais sans titre, n'a rien à dire : il ne
    /// laisse ni ligne vide ni icône orpheline.
    /// </summary>
    private static bool Visible(RpGlanceDto glance) =>
        glance.Active && !string.IsNullOrWhiteSpace(glance.Title);

    public static void DrawPreferences(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        using var card = Card.Begin("rpview_prefs", interactive: false);
        Layout.SectionHeader(l.RpProfilePreferences, Icons.Settings, tone: tone);
        BeginRows();

        Row(l.RpProfileLevel,    LevelLabel(p.RpLevel, l));
        Row(l.RpProfileApproach, ApproachLabel(p.ApproachMode, l));

        // Prise de contact et durée des scènes étaient servies par le serveur et
        // renseignées sur le site, mais n'étaient affichées nulle part en jeu.
        if (p.ContactMode is { Length: > 0 } contact)
            Row(l.RpProfileContact, ContactLabel(contact, l));
        if (p.SessionLength is { Length: > 0 } length)
            Row(l.RpProfileLengths, SessionLengthLabel(length, l));

        if (p.Languages.Length > 0)
            Row(l.RpProfileLanguages, string.Join(" / ", p.Languages.Select(LanguageLabel)));

        if (p.Themes.Length > 0)
        {
            Layout.Spacer(Theme.GapS);
            Text.Muted(l.RpProfileThemes);
            Layout.Spacer(Theme.GapXs);
            DrawThemeChips(p.Themes, ChipTone.Accent, tone);
        }

        if (p.AvoidThemes.Length > 0)
        {
            Layout.Spacer(Theme.GapS);
            Text.Muted(l.RpProfileAvoidThemes);
            Layout.Spacer(Theme.GapXs);
            DrawThemeChips(p.AvoidThemes, ChipTone.Danger);
        }
    }

    public static void DrawIdentity(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        var hasIdentity = p.Race is { Length: > 0 } || p.Age is { Length: > 0 }
                       || p.Origin is { Length: > 0 } || p.Occupation is { Length: > 0 }
                       || p.Pronouns is { Length: > 0 };
        if (!hasIdentity) return;

        using var card = Card.Begin("rpview_identity", interactive: false);
        Layout.SectionHeader(l.RpProfileIdentity, Icons.Profile, tone: tone);
        BeginRows();

        if (p.Race is { Length: > 0 } race)             Row(l.RpProfileRace, RaceLabel(race, l));
        if (p.Age is { Length: > 0 } age)               Row(l.RpProfileAge, age);
        if (p.Pronouns is { Length: > 0 } pronouns)     Row(l.RpProfilePronouns, pronouns);
        if (p.Origin is { Length: > 0 } origin)         Row(l.RpProfileOrigin, origin);
        if (p.Occupation is { Length: > 0 } occupation) Row(l.RpProfileOccupation, occupation);
    }

    public static void DrawTraits(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        var hasTraits = p.Height is { Length: > 0 } || p.Build is { Length: > 0 }
                     || p.Voice is { Length: > 0 } || p.Marks is { Length: > 0 };
        if (!hasTraits) return;

        using var card = Card.Begin("rpview_traits", interactive: false);
        Layout.SectionHeader(l.RpProfileTraits, Icons.Character, tone: tone);
        BeginRows();

        if (p.Height is { Length: > 0 } height) Row(l.RpProfileHeight, height);
        if (p.Build is { Length: > 0 } build)   Row(l.RpProfileBuild, build);
        if (p.Voice is { Length: > 0 } voice)   Row(l.RpProfileVoice, voice);
        if (p.Marks is { Length: > 0 } marks)   Row(l.RpProfileMarks, marks);
    }

    public static void DrawBelonging(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        var hasBelonging = p.FreeCompany is { Length: > 0 } || p.Allegiance is { Length: > 0 }
                        || p.Deity is { Length: > 0 };
        if (!hasBelonging) return;

        using var card = Card.Begin("rpview_belonging", interactive: false);
        Layout.SectionHeader(l.RpProfileBelonging, Icons.World, tone: tone);
        BeginRows();

        if (p.FreeCompany is { Length: > 0 } fc)        Row(l.RpProfileFreeCompany, fc);
        if (p.Allegiance is { Length: > 0 } allegiance) Row(l.RpProfileAllegiance, allegiance);
        if (p.Deity is { Length: > 0 } deity)           Row(l.RpProfileDeity, DeityLabel(deity, l));
    }

    /// <summary>
    /// Codes de sync. C'est la section dont l'usage est le plus concret en jeu :
    /// on croise quelqu'un, on veut son identifiant. D'où le bouton copier
    /// plutôt qu'un simple affichage, sur le modèle de la fiche d'établissement.
    ///
    /// Le champ arrive en chaîne JSON brute, comme pour les établissements, et
    /// vaut null quand la section dépasse l'audience du lecteur : rien à filtrer
    /// ici, le serveur l'a déjà fait.
    /// </summary>
    public static void DrawSyncshells(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        var entries = ParseSyncshells(p.Syncshells);
        if (entries.Length == 0) return;

        using var card = Card.Begin("rpview_sync", interactive: false);
        Layout.SectionHeader(l.RpProfileSyncshells, Icons.Copy, tone: tone);
        BeginRows();

        var expired = DateTime.UtcNow >= _syncCopiedUntil;

        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry.Id)) continue;

            if (i > 0) Layout.Spacer(Theme.GapXs);

            Text.Small(SyncshellLabel(entry, l));
            ImGui.SameLine(Theme.S(140f));
            Text.Body(entry.Id);

            var copied = !expired && _syncCopiedKey == i;
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (ImGui.SmallButton((copied ? l.EstabCopied : l.RpProfileSyncCopy) + "##rpsync_" + i))
            {
                ImGui.SetClipboardText(entry.Id);
                _syncCopiedKey   = i;
                _syncCopiedUntil = DateTime.UtcNow.AddSeconds(2);
            }
        }
    }

    // Retour visuel du bouton copier. Statique comme le reste de la vue : une
    // seule fiche est affichée à la fois, et l'état ne survit pas aux 2 secondes.
    private static int      _syncCopiedKey   = -1;
    private static DateTime _syncCopiedUntil = DateTime.MinValue;

    /// <summary>
    /// Désérialise la chaîne stockée. Illisible, elle rend un tableau vide : une
    /// fiche mal formée ne doit pas empêcher d'afficher le reste.
    /// </summary>
    private static SyncshellEntryDto[] ParseSyncshells(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<SyncshellEntryDto[]>(json) ?? [];
        }
        catch { return []; }
    }

    /// <summary>
    /// Libellé d'un service. Plus large que la liste proposée à la saisie : les
    /// services retirés du formulaire restent lisibles sur les fiches anciennes.
    /// </summary>
    public static string SyncshellLabel(SyncshellEntryDto s, Loc l) => s.Type switch
    {
        "snowcloak" => "Snowcloak",
        "lightless" => "Lightless",
        "umbra"     => "Umbra",
        "glamourer" => "Glamourer",
        "mare"      => "Mare Synchronos",
        "lightsync" => "Lightsync",
        "autre"     => !string.IsNullOrEmpty(s.Name) ? s.Name : l.RpProfileSyncshellOther,
        var other   => other,
    };

    /// <summary>
    /// Relations. Toujours en consultation : les nouer se fait sur le site, où
    /// l'on dispose du clavier et de la recherche de personnages.
    /// </summary>
    public static void DrawRelations(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        if (p.Relations.Length == 0) return;

        using var card = Card.Begin("rpview_relations", interactive: false);
        Layout.SectionHeader(l.RpProfileRelations, Icons.Around, p.Relations.Length, tone: tone);

        foreach (var relation in p.Relations)
        {
            Chip.Draw(RelationLabel(relation.Kind, l), ChipTone.Accent);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            ImGui.AlignTextToFramePadding();
            Text.Body(relation.TargetName);

            if (relation.Note is { Length: > 0 } note)
                Text.Small(note);

            Layout.Spacer(Theme.GapXs);
        }
    }

    /// <summary>
    /// Blocs rédigés de la fiche.
    ///
    /// Ils portent icône et teinte comme toutes les autres sections : sans elles,
    /// deux familles de cartes cohabitaient dans la même fenêtre, les unes
    /// titrées avec une icône dans la couleur de la fiche, les autres nues et
    /// dans le bleu par défaut du thème.
    ///
    /// « Limites » fait exception et garde la couleur d'alerte : c'est un
    /// avertissement adressé au lecteur, pas une rubrique de plus.
    /// </summary>
    public static void DrawStory(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        DrawTextBlock("rpview_appearance",  l.RpProfileAppearance,  p.Appearance,
                      Icons.Diamond, tone);
        DrawTextBlock("rpview_personality", l.RpProfilePersonality, p.Personality,
                      Icons.RpLive, tone);
        DrawTextBlock("rpview_background",  l.RpProfileBackground,  p.Background,
                      Icons.Clock, tone);
        DrawTextBlock("rpview_limits",      l.RpProfileLimits,      p.Limits,
                      Icons.Warning, Theme.Danger);
    }

    /// <summary>
    /// Thème musical et lien externe, jusqu'ici saisissables sur le site sans
    /// jamais apparaître en jeu.
    ///
    /// L'adresse est affichée en toutes lettres à côté du bouton : ce sont des
    /// liens écrits par un autre joueur, on doit voir où l'on va avant de sortir
    /// du jeu.
    /// </summary>
    public static void DrawLinks(RpProfileDto p, Loc l, Vector4? tone = null)
    {
        var hasLinks = p.ThemeSongUrl is { Length: > 0 } || p.ExternalUrl is { Length: > 0 };
        if (!hasLinks) return;

        using var card = Card.Begin("rpview_links", interactive: false);
        Layout.SectionHeader(l.RpProfileLinks, Icons.External, tone: tone);

        if (p.ThemeSongUrl is { Length: > 0 } song)
            DrawLink("rpview_song", l.RpProfileThemeSong, song, l);

        if (p.ExternalUrl is { Length: > 0 } external)
        {
            if (p.ThemeSongUrl is { Length: > 0 }) Layout.Spacer(Theme.GapS);
            DrawLink("rpview_ext", l.RpProfileExternalLink, external, l);
        }
    }

    private static void DrawLink(string id, string label, string url, Loc l)
    {
        Text.Muted(label);
        Layout.Spacer(Theme.GapXs);
        Text.Small(url);
        Layout.Spacer(Theme.GapXs);

        if (Btn.Draw(l.RpProfileOpenLink, BtnTone.Ghost, BtnSize.Small, Icons.External, id: id))
            OpenUrl(url);
    }

    /// <summary>
    /// Ouvre une adresse dans le navigateur. Seuls http et https sont suivis :
    /// l'adresse vient d'un autre joueur, et un schéma exotique ne doit pas
    /// pouvoir lancer autre chose qu'une page web.
    /// </summary>
    private static void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;

        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    /// <param name="icon">
    /// Icône du titre de section, comme pour toutes les autres cartes de la
    /// fiche. Nulle, l'en-tête n'en affiche pas.
    /// </param>
    public static void DrawTextBlock(string id, string title, string? body,
                                     FontAwesomeIcon? icon = null, Vector4? tone = null)
    {
        if (string.IsNullOrWhiteSpace(body)) return;

        using var card = Card.Begin(id, interactive: false);
        Layout.SectionHeader(title, icon, tone: tone);
        MarkdownView.Draw(body, Theme.TextMuted);
    }

    // ─── Rendu utilitaire ─────────────────────────────────────────────────────

    /// <summary>
    /// Vrai tant qu'aucune ligne n'a été dessinée dans la carte en cours.
    ///
    /// Le filet se dessine avant chaque ligne sauf la première : un filet en tête
    /// reviendrait à souligner le titre de section, ce que la maquette écarte.
    /// État statique assumé, le rendu ImGui étant séquentiel et mono-thread.
    /// </summary>
    private static bool _firstRow = true;

    /// <summary>
    /// Réarme le suivi des filets. À appeler après chaque SectionHeader : sans
    /// cela, la première ligne d'une carte hérite du filet de la précédente.
    /// </summary>
    public static void BeginRows() => _firstRow = true;

    /// <summary>Filet fin pleine largeur, dans la teinte de bordure discrète.</summary>
    private static void RowSeparator()
    {
        Layout.Spacer(Theme.GapXs);

        var dl    = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X - Card.RightInset;

        dl.AddLine(start, new Vector2(start.X + width, start.Y),
                   ImGui.GetColorU32(Theme.BorderSoft), 1f);

        Layout.Spacer(Theme.GapXs);
    }

    public static void Row(string label, string value)
    {
        if (!_firstRow) RowSeparator();
        _firstRow = false;

        Text.Small(label);
        ImGui.SameLine(Theme.S(140f));
        Text.Body(value);
    }

    /// <summary>
    /// Chips de thèmes. `tint` l'emporte sur `tone` quand il est renseigné : la
    /// couleur de la fiche prime sur la couleur générique. Les thèmes évités
    /// restent volontairement sans teinte, le sens y comptant plus que
    /// l'habillage.
    /// </summary>
    public static void DrawThemeChips(string[] themes, ChipTone tone, Vector4? tint = null)
    {
        for (var i = 0; i < themes.Length; i++)
        {
            if (i > 0) ImGui.SameLine(0f, Theme.S(Theme.GapXs));

            if (tint is { } color) Chip.Colored(ThemeLabel(themes[i], Plugin.L), color);
            else                   Chip.Draw(ThemeLabel(themes[i], Plugin.L), tone);
        }
    }

    // ─── Libellés de vocabulaire ──────────────────────────────────────────────

    public static string LevelLabel(string key, Loc l) => key switch
    {
        "beginner"  => l.RpProfileLevelBeginner,
        "casual"    => l.RpProfileLevelCasual,
        "confirmed" => l.RpProfileLevelConfirmed,
        _           => key,
    };

    public static string IcStateLabel(string key, Loc l) => key switch
    {
        "ic"  => l.RpProfileIcStateIc,
        "ooc" => l.RpProfileIcStateOoc,
        _     => key,
    };

    /// <summary>
    /// Teinte de la pastille d'état. Elle porte l'essentiel du message : la
    /// couleur se lit d'un coup d'œil dans une liste là où le libellé demande
    /// d'être lu. « En RP » est une invitation à jouer, d'où le vert ; « hors
    /// RP » n'est ni un problème ni une invitation, d'où le neutre. Un état
    /// inconnu, venu d'un serveur plus récent, reste neutre plutôt que de
    /// prendre une couleur au hasard.
    /// </summary>
    public static ChipTone IcStateTone(string key) => key switch
    {
        "ic" => ChipTone.Success,
        _    => ChipTone.Neutral,
    };

    public static string ApproachLabel(string key, Loc l) => key switch
    {
        "come_to_me" => l.RpProfileApproachCome,
        "i_approach" => l.RpProfileApproachIGo,
        "either"     => l.RpProfileApproachEither,
        _            => key,
    };

    public static string ContactLabel(string key, Loc l) => key switch
    {
        "direct"     => l.RpProfileContactDirect,
        "tell_first" => l.RpProfileContactTell,
        "either"     => l.RpProfileContactEither,
        _            => key,
    };

    public static string SessionLengthLabel(string key, Loc l) => key switch
    {
        "short"  => l.RpProfileLengthShort,
        "medium" => l.RpProfileLengthMedium,
        "long"   => l.RpProfileLengthLong,
        _        => key,
    };

    /// <summary>Les noms de langue s'écrivent dans leur propre langue.</summary>
    public static string LanguageLabel(string key) => key switch
    {
        "fr" => "Français",
        "en" => "English",
        _    => key,
    };

    /// <summary>
    /// Nom de race traduit. Les orthographes diffèrent d'une langue à l'autre
    /// (« Elézen » contre « Elezen »), et une valeur inconnue s'affiche telle
    /// quelle, pour qu'un ajout côté serveur se voie au lieu de se fondre.
    /// </summary>
    public static string RaceLabel(string key, Loc l) =>
        l.RpRaceLabels.TryGetValue(key, out var label) ? label : key;

    public static string DeityLabel(string key, Loc l) => key switch
    {
        ""         => l.RpProfileDeityNone,
        "halone"   => "Halone",
        "menphina" => "Menphina",
        "thaliak"  => "Thaliak",
        "nymeia"   => "Nymeia",
        "llymlaen" => "Llymlaen",
        "oschon"   => "Oschon",
        "byregot"  => "Byregot",
        "rhalgr"   => "Rhalgr",
        "azeyma"   => "Azeyma",
        "naldthal" => "Nald'thal",
        "nophica"  => "Nophica",
        "althyk"   => "Althyk",
        "other"    => l.RpProfileRaceOther,
        _          => key,
    };

    public static string RelationLabel(string key, Loc l) => key switch
    {
        "ally"    => l.RpProfileRelationAlly,
        "friend"  => l.RpProfileRelationFriend,
        "family"  => l.RpProfileRelationFamily,
        "lover"   => l.RpProfileRelationLover,
        "mentor"  => l.RpProfileRelationMentor,
        "student" => l.RpProfileRelationStudent,
        "rival"   => l.RpProfileRelationRival,
        "enemy"   => l.RpProfileRelationEnemy,
        "other"   => l.RpProfileRelationOther,
        // Une valeur inconnue s'affiche telle quelle plutôt que sous « Autre » :
        // un type de relation ajouté côté serveur doit se voir, pas se déguiser
        // en catégorie existante.
        _         => key,
    };

    /// <summary>
    /// Nom de thème traduit. Huit des douze diffèrent entre les deux langues,
    /// et une valeur inconnue s'affiche telle quelle.
    /// </summary>
    public static string ThemeLabel(string key, Loc l) =>
        l.RpThemeLabels.TryGetValue(key, out var label) ? label : key;
}
