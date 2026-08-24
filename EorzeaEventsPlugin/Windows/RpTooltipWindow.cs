using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Aperçu de fiche RP affiché sur le joueur ciblé ou survolé, à la manière de
/// Total RP 3 : qui est ce personnage, dans quel état de jeu il se trouve et ce
/// qu'on remarquerait de lui avant même de lui adresser la parole.
///
/// Une fenêtre plutôt qu'un tooltip ImGui : un tooltip appartient à l'élément
/// survolé et ne peut naître que d'un survol d'interface, or la cible est ici un
/// objet du jeu et l'aperçu doit survivre à une cible dure que rien ne survole.
///
/// <c>NoInputs</c> n'est pas un confort : sans lui la fenêtre se poserait entre
/// le joueur et sa cible, et volerait le clic de ciblage suivant. Elle ne réagit
/// donc à rien, ce qui rend aussi inoffensifs les appels à
/// <c>Feedback.TooltipOnHover</c> nichés dans le coup d'œil.
///
/// Rien de ce qui s'affiche ici ne déclenche de requête : l'alimentation vient du
/// seul cache tenu par <see cref="Plugin.AvailableEntries"/>. Voir
/// <c>Plugin.UpdateRpTooltip</c> pour pourquoi ce point n'est pas négociable.
/// </summary>
public sealed class RpTooltipWindow : ThemedWindow
{
    /// <summary>
    /// Largeur imposée au contenu. <c>AlwaysAutoResize</c> suivrait sinon la
    /// longueur du statut du moment, et l'infobulle changerait de largeur à
    /// chaque personnage survolé.
    /// </summary>
    private const float ContentWidth = 250f;

    /// <summary>Écart au curseur, assez large pour ne pas passer sous la main de la souris.</summary>
    private const float CursorGap = 20f;

    /// <summary>Écart sous les pieds de la cible, pour ne pas masquer son nameplate.</summary>
    private const float TargetGap = 14f;

    /// <summary>
    /// Déplacement en deçà duquel l'ancre ne bouge pas. Une position recalculée
    /// à chaque frame fait vibrer la fenêtre : la souris n'est jamais tout à fait
    /// immobile, et une cible debout respire sur place.
    /// </summary>
    private const float DeadZone = 5f;

    private RpAvailabilityEntryDto? _entry;
    private ulong                   _subject;
    private bool                    _fromMouse;
    private Vector3                 _worldPos;
    private Vector2?                _anchor;
    private Vector2                 _size;

    /// <summary>
    /// Plus opaque que les fenêtres du plugin : celle-ci se pose sur le décor en
    /// mouvement, où le fond translucide habituel rendrait le texte illisible.
    /// </summary>
    protected override float BackgroundOpacity => 0.97f;

    public RpTooltipWindow()
        : base("##rptooltip",
               ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize
               | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
               | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing
               | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoInputs)
    {
        // Échap appartient au jeu tant que l'infobulle est à l'écran : la fermer
        // ne servirait à rien, la boucle de framework la rouvrirait à la frame
        // suivante, et le joueur aurait perdu son annulation.
        RespectCloseHotkey = false;
    }

    /// <summary>
    /// Sujet à afficher, alimenté depuis la boucle de framework.
    /// </summary>
    /// <param name="subject">
    /// Identifiant de l'objet de jeu visé. Changer de sujet libère l'ancre : sans
    /// cela, la fenêtre glisserait de l'ancien personnage vers le nouveau au lieu
    /// d'apparaître directement sur lui.
    /// </param>
    /// <param name="fromMouse">
    /// Vrai pour un survol, faux pour la cible dure. Décide de l'ancrage : le
    /// curseur dans le premier cas, les pieds du personnage dans le second, la
    /// souris n'ayant alors aucune raison d'être sur lui.
    /// </param>
    public void Show(RpAvailabilityEntryDto entry, ulong subject, bool fromMouse, Vector3 worldPos)
    {
        if (subject != _subject || fromMouse != _fromMouse) _anchor = null;

        _subject   = subject;
        _fromMouse = fromMouse;
        _worldPos  = worldPos;
        _entry     = entry;
        IsOpen     = true;
    }

    /// <summary>
    /// Plus rien à montrer. Appelé aussi bien quand la cible disparaît que quand
    /// elle n'a pas de fiche visible : les deux cas doivent être indiscernables.
    /// </summary>
    public void Clear()
    {
        if (_entry == null && !IsOpen) return;

        _entry   = null;
        _subject = 0;
        _anchor  = null;
        IsOpen   = false;
    }

    /// <summary>
    /// Le modificateur se lit ici et non dans la boucle de framework : l'état du
    /// clavier appartient au contexte ImGui, qui n'existe que pendant le rendu.
    /// </summary>
    public override bool DrawConditions() =>
        _entry?.Profile != null && ModifierHeld();

    private static bool ModifierHeld() => Plugin.Config.RpTooltipModifier switch
    {
        RpTooltipKey.Ctrl => ImGui.GetIO().KeyCtrl,
        RpTooltipKey.Alt  => ImGui.GetIO().KeyAlt,
        _                 => true,
    };

    public override void PreDraw()
    {
        base.PreDraw();

        if (Resolve() is not { } candidate) return;

        if (_anchor is not { } current
            || Vector2.Distance(current, candidate) > Theme.S(DeadZone))
            _anchor = candidate;

        ImGui.SetNextWindowPos(Clamp(_anchor.Value));
    }

    /// <summary>
    /// Position visée à cette frame, ou null quand elle n'est pas calculable :
    /// l'ancre précédente vaut alors mieux qu'un saut dans un coin de l'écran.
    /// </summary>
    private Vector2? Resolve()
    {
        if (_fromMouse)
        {
            var mouse = ImGui.GetIO().MousePos;

            // ImGui renvoie une position très négative quand le curseur quitte la
            // fenêtre du jeu : la suivre poserait l'infobulle hors de l'écran.
            if (float.IsNaN(mouse.X) || mouse.X < -1000f) return null;

            return mouse + Theme.S(CursorGap, CursorGap);
        }

        if (!Plugin.GameGui.WorldToScreen(_worldPos, out var screen)) return null;

        // Centrée sous la cible, sur la largeur mesurée à la frame précédente :
        // AlwaysAutoResize ne la donne pas à l'avance.
        return new Vector2(screen.X - _size.X * 0.5f, screen.Y + Theme.S(TargetGap));
    }

    /// <summary>
    /// Maintient la fenêtre entière dans l'espace utile. Sans cela, une cible au
    /// bord de l'écran fait sortir la moitié du contenu du champ visible.
    /// </summary>
    private Vector2 Clamp(Vector2 position)
    {
        var viewport = ImGui.GetMainViewport();
        var min      = viewport.WorkPos;
        var max      = viewport.WorkPos + viewport.WorkSize - _size;

        return new Vector2(Math.Clamp(position.X, min.X, MathF.Max(min.X, max.X)),
                           Math.Clamp(position.Y, min.Y, MathF.Max(min.Y, max.Y)));
    }

    public override void Draw()
    {
        if (_entry is not { Profile: { } profile } entry) return;

        // Relevée pendant le rendu : c'est la seule taille dont on dispose pour
        // centrer et borner la fenêtre à la frame suivante.
        _size = ImGui.GetWindowSize();

        var l       = Plugin.L;
        var accent  = RpProfileView.Accent(profile);
        var accent2 = RpProfileView.Accent2(profile);

        var wrapAt = ImGui.GetCursorPosX() + Theme.S(ContentWidth);
        ImGui.PushTextWrapPos(wrapAt);

        // Impose la largeur avant tout contenu, sinon l'infobulle se rétrécit sur
        // un nom court puis s'élargit sur le suivant.
        ImGui.Dummy(new Vector2(Theme.S(ContentWidth), 0f));

        // Le nom RP prime sur le nom de personnage : c'est celui sous lequel on
        // s'adressera à lui. Le second reste dessous, il sert à le retrouver.
        var displayName = profile.RpName is { Length: > 0 } rpName ? rpName : entry.CharacterName;

        Text.H2(displayName);
        AnimatedText.Draw(profile.RpTitle, accent2, profile.TitleAnimation, accent);
        Text.Small($"{entry.CharacterName} · {entry.Server}");

        // Le marquage sensible se voit toujours, son contenu non : c'est
        // l'avertissement qui permet de décider d'ouvrir la fiche ou pas.
        var masked = profile.Nsfw && !Plugin.Config.ShowNsfwProfiles;

        if (profile.Nsfw)
        {
            Layout.Spacer(Theme.GapS);
            Chip.Draw(l.RpProfileNsfw, ChipTone.Danger, Icons.Warning);
            if (masked) Text.Small(l.RpTooltipNsfwHidden);
        }

        if (!masked) DrawPresent(profile, accent, l);

        DrawChips(profile, accent, l, wrapAt);

        ImGui.PopTextWrapPos();
    }

    /// <summary>
    /// Instant présent et coup d'œil, c'est-à-dire tout ce qui se périme dans la
    /// soirée. En tête du corps parce que c'est ce sur quoi se décide un abord :
    /// le niveau et les langues, eux, ne changent jamais.
    /// </summary>
    private static void DrawPresent(RpProfileDto profile, Vector4 accent, Loc l)
    {
        if (profile.IcState is { Length: > 0 } state)
        {
            Layout.Spacer(Theme.GapS);
            Chip.Draw(RpProfileView.IcStateLabel(state, l),
                      RpProfileView.IcStateTone(state), Icons.RpLive);
        }

        if (profile.Currently is { Length: > 0 } currently)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Body(currently);
        }

        if (!RpProfileView.HasGlances(profile)) return;

        Layout.Divider(Theme.GapS);
        RpProfileView.GlanceRows(profile, accent);
    }

    /// <summary>
    /// Niveau, mode d'approche et langues : les trois critères sur lesquels on
    /// décide comment aborder quelqu'un, et les seuls que l'infobulle promet.
    /// Chacun ne reste sur la ligne que s'il y tient, la largeur étant contrainte.
    /// </summary>
    private static void DrawChips(RpProfileDto profile, Vector4 accent, Loc l, float limit)
    {
        Layout.Spacer(Theme.GapS);

        var gap = Theme.S(Theme.GapXs);

        void SameLineIfRoom(float width)
        {
            if (ImGui.GetCursorPosX() + gap + width <= limit) ImGui.SameLine(0f, gap);
        }

        // Teintée par la fiche, comme sur l'entête : c'est le seul rappel de
        // l'habillage de son auteur dans un aperçu sans portrait ni bannière.
        Chip.Colored(RpProfileView.LevelLabel(profile.RpLevel, l), accent);

        var approach = RpProfileView.ApproachLabel(profile.ApproachMode, l);
        SameLineIfRoom(Chip.Measure(approach));
        Chip.Draw(approach, ChipTone.Accent);

        if (profile.Languages.Length == 0) return;

        var languages = string.Join(" / ", profile.Languages.Select(RpProfileView.LanguageLabel));
        SameLineIfRoom(Chip.Measure(languages));
        Chip.Draw(languages, ChipTone.Neutral);
    }
}
