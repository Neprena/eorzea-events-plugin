using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Shell;

/// <summary>Geste demandé depuis la barre de titre.</summary>
internal enum TitleAction
{
    None,
    Close,

    /// <summary>Réduire la fenêtre à sa barre, ou la restaurer.</summary>
    ToggleFold,

    /// <summary>Le compteur « autour de moi » de la barre réduite : restaurer sur cette page.</summary>
    OpenAround,
}

/// <summary>
/// Barre de titre maison, en remplacement de celle de Dalamud.
///
/// Elle assume le déplacement de la fenêtre, que <c>NoTitleBar</c> supprime.
/// Le redimensionnement, lui, reste natif : la fenêtre ne pose pas
/// <c>NoResize</c>, ImGui gère donc les bords et la poignée.
///
/// Fenêtre réduite, la barre reste seule à l'écran. Elle porte alors ce dont on
/// a besoin en jouant sans la grande fenêtre : se déclarer disponible, et voir
/// combien de joueurs le sont autour de soi.
/// </summary>
internal static class TitleBar
{
    /// <summary>Dessine la barre. Retourne le geste demandé, s'il y en a un.</summary>
    /// <param name="large">Le grand logo des saisons : à peindre ensuite par <see cref="DrawLargeLogo"/>.</param>
    public static TitleAction Draw(float width, bool folded, bool large = false)
    {
        var height = Theme.S(Theme.TitleBarHeight);
        var origin = ImGui.GetCursorScreenPos();
        var end    = new Vector2(origin.X + width, origin.Y + height);
        var dl     = ImGui.GetWindowDrawList();

        // Un cran plus opaque que le contenu. Le rayon doit être celui de la
        // fenêtre, sinon le fond déborde aux angles ; réduite, la barre est toute
        // la fenêtre et s'arrondit aussi en bas.
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Theme.Alpha(Theme.BgSidebar, AppShell.ChromeOpacity(0.10f))),
                         Theme.S(Theme.RadiusWindow), folded ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersTop);

        if (!folded)
            dl.AddLine(new Vector2(origin.X, end.Y - 0.5f), new Vector2(end.X, end.Y - 0.5f),
                       ImGui.GetColorU32(Theme.Border), 1f);

        // Boutons carrés calés sur la hauteur de la barre, et non sur la hauteur
        // d'un cadre ImGui : celle-ci dépend de la police et du padding, si bien
        // que le fond ne tombait jamais au centre du bandeau. Marge identique en
        // haut, en bas et à droite.
        var margin  = MathF.Round(Theme.S(Theme.GapS));
        var side    = MathF.Round(height - margin * 2f);
        var gap     = MathF.Round(Theme.S(Theme.GapXs));
        var daily   = folded ? null : Seasons.SeasonHunts.ActiveDaily;
        var buttons = (folded ? 2 : 3) + (daily != null ? 1 : 0);
        var right   = MathF.Round(end.X - margin);
        var top     = MathF.Round(origin.Y + margin);

        var action = TitleAction.None;

        // ── Zone de déplacement ───────────────────────────────────────────────
        // Tout ce qui précède les boutons ; réduite, la marque seulement, les
        // commandes qui la suivent devant rester cliquables.
        var brandEnd = DrawBrand(dl, origin, height, large && !folded);
        var dragEnd  = folded ? brandEnd : right - (side + gap) * buttons;

        // Guirlande ou compte à rebours, dans le creux entre la marque et les boutons.
        if (!folded) Seasons.SeasonDecor.TitleBar(dl, brandEnd, dragEnd, origin.Y, height);

        ImGui.SetCursorScreenPos(origin);
        ImGui.InvisibleButton("##titledrag", new Vector2(Math.Max(1f, dragEnd - origin.X), height));
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            ImGui.SetWindowPos(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta);

        // ── Commandes de la barre réduite ─────────────────────────────────────
        if (folded && DrawFoldedControls(dl, new Vector2(brandEnd + Theme.S(Theme.GapL), origin.Y), height))
            action = TitleAction.OpenAround;

        // ── Actions ───────────────────────────────────────────────────────────
        var x = right - side;

        if (IconButton(dl, new Vector2(x, top), side, Icons.Close, "shell_close"))
            action = TitleAction.Close;

        x -= side + gap;
        if (IconButton(dl, new Vector2(x, top), side, folded ? Icons.Unfold : Icons.Fold, "shell_fold",
                       folded ? Plugin.L.WindowRestore : Plugin.L.WindowFold))
            action = TitleAction.ToggleFold;

        if (!folded)
        {
            x -= side + gap;
            if (IconButton(dl, new Vector2(x, top), side, Icons.External, "shell_site", Plugin.L.ViewOnline))
                OpenSite();
        }

        // Le calendrier du moment (cadeau de l'Avent, cœur des billets doux),
        // avec une pastille tant que la case du jour n'est pas ouverte.
        if (daily != null)
        {
            x -= side + gap;
            if (IconButton(dl, new Vector2(x, top), side, daily.Icon, "shell_daily", daily.Title(Plugin.L)))
                Seasons.SeasonOverlay.ToggleDaily();

            if (Seasons.SeasonOverlay.DailyUnread)
                dl.AddCircleFilled(new Vector2(x + side - Theme.S(7f), top + Theme.S(7f)), Theme.S(3.5f),
                                   ImGui.GetColorU32(Theme.Danger));
        }

        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
        return action;
    }

    /// <summary>
    /// Largeur de la barre réduite, en pixels écran : marque, commandes et
    /// boutons, sans rien de plus. La fenêtre prend exactement cette largeur.
    /// </summary>
    public static float FoldedWidth()
    {
        var height = Theme.S(Theme.TitleBarHeight);
        var margin = MathF.Round(Theme.S(Theme.GapS));
        var side   = MathF.Round(height - margin * 2f);
        var gap    = MathF.Round(Theme.S(Theme.GapXs));

        float brand;
        using (Fonts.PushBrand())
            brand = ImGui.CalcTextSize(BrandName).X;

        var width = Theme.S(Theme.PadWindowX) + LogoSide(height) + Theme.S(Theme.GapS) + brand
                    + Theme.S(Theme.GapL) + FoldedControlsWidth()
                    + Theme.S(Theme.GapM) + (side + gap) * 2f + margin;

        return MathF.Round(width);
    }

    private const string BrandName = "Eorzea Events";

    private static float LogoSide(float height) => MathF.Round(height - Theme.S(Theme.GapM) * 2f + Theme.S(4f));

    // ─── Grand logo des saisons ───────────────────────────────────────────────
    //
    // Pendant un thème de saison, le logo double et devient un médaillon : il
    // chevauche la barre et pend sur le haut de la colonne, dont les entrées
    // descendent d'autant (LogoOverhang). Abaissé de quelques pixels, il laisse
    // au chapeau ou au bonnet la place de tenir dans la fenêtre.

    /// <summary>Haut du grand logo sous le haut de la barre, en pixels à 100 %.</summary>
    private const float LargeLogoTop = 14f;

    private static float LargeSide(float height) => LogoSide(height) * 2f;

    /// <summary>Coin du grand logo réservé par la barre de cette image, ou null.</summary>
    private static Vector2? _largeLogo;

    /// <summary>Ce que le grand logo descend sous la barre : la colonne commence ses entrées plus bas d'autant.</summary>
    public static float LogoOverhang { get; private set; }

    /// <summary>
    /// Peint le grand logo et ses ornements, une fois la colonne dessinée :
    /// une fenêtre enfant créée après elle passe au-dessus. Elle ne prend
    /// aucune entrée, la barre se déplace et la colonne se clique au travers.
    /// </summary>
    public static void DrawLargeLogo()
    {
        if (_largeLogo is not { } min) return;

        var side = LargeSide(Theme.S(Theme.TitleBarHeight));

        // Assez de marge autour pour les ornements, qui débordent du carré.
        var area = min - new Vector2(side * 0.5f, side * 0.5f);
        ImGui.SetCursorScreenPos(area);

        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var child = ImRaii.Child("##season_logo", new Vector2(side * 2f, side * 2f), false,
                                       ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground
                                       | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!child) return;

        var dl = ImGui.GetWindowDrawList();
        Seasons.SeasonDecor.LogoBehind(dl, min, side);
        Brand.Draw(dl, min, side);
        Seasons.SeasonDecor.LogoFront(dl, min, side);
    }

    /// <summary>Logo puis nom, en capitales dorées. Retourne l'abscisse de fin.</summary>
    private static float DrawBrand(ImDrawListPtr dl, Vector2 origin, float height, bool large)
    {
        var x    = origin.X + Theme.S(Theme.PadWindowX);
        var logo = LogoSide(height);

        if (large)
        {
            // Le grand logo ne fait que réserver sa place ici : il déborde sur la
            // colonne, qui le recouvrirait. AppShell le peint après elle.
            logo         = LargeSide(height);
            _largeLogo   = new Vector2(x, MathF.Round(origin.Y + Theme.S(LargeLogoTop)));
            LogoOverhang = MathF.Max(0f, _largeLogo.Value.Y + logo - (origin.Y + height));
        }
        else
        {
            _largeLogo   = null;
            LogoOverhang = 0f;

            // Tant que la texture n'est pas chargée, sa place reste réservée : le
            // nom ne doit pas sauter de côté une image plus tard.
            var logoMin = new Vector2(x, MathF.Round(origin.Y + (height - logo) * 0.5f));
            Seasons.SeasonDecor.LogoBehind(dl, logoMin, logo);
            Brand.Draw(dl, logoMin, logo);
            Seasons.SeasonDecor.LogoFront(dl, logoMin, logo);
        }

        x += logo + Theme.S(Theme.GapS);

        using var font = Fonts.PushBrand();
        var size = ImGui.CalcTextSize(BrandName);
        dl.AddText(new Vector2(x, origin.Y + (height - size.Y) * 0.5f), ImGui.GetColorU32(Theme.TitleGold), BrandName);

        return x + size.X;
    }

    // ─── Barre réduite ────────────────────────────────────────────────────────

    /// <summary>
    /// L'interrupteur n'a de sens qu'avec un personnage lié : sans lui, la
    /// déclaration partirait en 401. Le compteur, lui, sert à tous.
    /// </summary>
    private static bool CanDeclare => Plugin.CurrentCharacter != null && Plugin.Api.HasToken;

    private static float FoldedControlsWidth()
    {
        var width = CounterWidth();
        if (CanDeclare) width += SwitchWidth() + Theme.S(Theme.GapS);
        return width;
    }

    private static float SwitchWidth() =>
        Theme.S(32f + Theme.GapS) + ImGui.CalcTextSize(Plugin.L.FoldAvailable).X + Theme.S(16f);

    private static float CounterWidth() =>
        ImGui.CalcTextSize($"{Icons.Around.S()} {Plugin.AvailableEntries.Count}").X + Theme.S(20f);

    /// <summary>Interrupteur de disponibilité puis compteur. Rend vrai si le compteur est cliqué.</summary>
    private static bool DrawFoldedControls(ImDrawListPtr dl, Vector2 origin, float height)
    {
        var x   = origin.X;
        var mid = origin.Y + height * 0.5f;

        if (CanDeclare)
        {
            var wanted = Plugin.CurrentCharacterAvailabilityWanted;
            var w      = SwitchWidth();
            var min    = new Vector2(x, mid - Theme.S(15f));

            ImGui.SetCursorScreenPos(min);
            if (ImGui.InvisibleButton("##fold_available", new Vector2(w, Theme.S(30f))))
                Plugin.SetRpAvailability(!wanted);

            var hovered = ImGui.IsItemHovered();
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                dl.AddRectFilled(min, min + new Vector2(w, Theme.S(30f)),
                                 ImGui.GetColorU32(Theme.Alpha(Theme.Text, 0.08f)), Theme.S(Theme.RadiusFrame));
                Feedback.Tooltip(Plugin.L.FoldAvailableHint);
            }

            // Le bouton glisse d'un bord à l'autre au lieu d'y sauter.
            var on     = Motion.Approach(ImGui.GetID("##fold_available_knob"), wanted ? 1f : 0f, Motion.Fast);
            var track  = new Vector2(x + Theme.S(8f), mid - Theme.S(9f));
            var trackE = track + Theme.S(32f, 18f);
            dl.AddRectFilled(track, trackE,
                             ImGui.GetColorU32(Theme.Mix(Theme.Alpha(Theme.Text, 0.2f), Theme.Online, on)),
                             Theme.S(9f));
            dl.AddCircleFilled(new Vector2(track.X + Theme.S(9f + 14f * on), mid), Theme.S(7f),
                               ImGui.GetColorU32(Vector4.One));

            var label = Plugin.L.FoldAvailable;
            var size  = ImGui.CalcTextSize(label);
            dl.AddText(new Vector2(trackE.X + Theme.S(Theme.GapS), mid - size.Y * 0.5f),
                       ImGui.GetColorU32(Theme.Text), label);

            x += w + Theme.S(Theme.GapS);
        }

        // Compteur en pilule de cristal : c'est l'information qu'on surveille
        // en jouant, et le chemin le plus court vers la liste.
        var count    = Plugin.AvailableEntries.Count;
        var text     = $"{Icons.Around.S()} {count}";
        var textSize = ImGui.CalcTextSize(text);
        var pillMin  = new Vector2(x, mid - Theme.S(13f));
        var pillMax  = new Vector2(x + CounterWidth(), mid + Theme.S(13f));

        ImGui.SetCursorScreenPos(pillMin);
        var clicked = ImGui.InvisibleButton("##fold_around", pillMax - pillMin);
        var over    = ImGui.IsItemHovered();
        if (over)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            Feedback.Tooltip(string.Format(Plugin.L.FoldAroundHint, count));
        }

        dl.AddRectFilled(pillMin, pillMax, ImGui.GetColorU32(Theme.Alpha(Theme.Accent, over ? 0.22f : 0.12f)),
                         Theme.S(13f));
        dl.AddRect(pillMin, pillMax, ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0.3f)), Theme.S(13f),
                   ImDrawFlags.None, 1f);
        dl.AddText(pillMin + (pillMax - pillMin - textSize) * 0.5f, ImGui.GetColorU32(Theme.AccentHover), text);

        return clicked;
    }

    /// <summary>
    /// Bouton d'icône dessiné à la main.
    ///
    /// <c>ImGui.Button</c> dimensionne son fond à partir du cadre courant, donc
    /// de la police et du padding : sur un bandeau de hauteur fixe, il ne tombait
    /// pas au centre. Ici le carré est posé aux coordonnées voulues, et le glyphe
    /// centré sur son encombrement réel.
    /// </summary>
    private static bool IconButton(ImDrawListPtr dl, Vector2 position, float side,
                                   FontAwesomeIcon icon, string id, string? tooltip = null)
    {
        ImGui.SetCursorScreenPos(position);
        var clicked = ImGui.InvisibleButton($"##{id}", new Vector2(side, side));
        var hovered = ImGui.IsItemHovered();

        // Le survol s'allume en fondu, comme les entrées de la colonne.
        var light = Motion.Approach(ImGui.GetID($"##{id}_light"), hovered ? 1f : 0f, Motion.Fast);
        if (light > 0.01f)
            dl.AddRectFilled(position, position + new Vector2(side, side),
                             ImGui.GetColorU32(Theme.Alpha(Theme.Text, 0.14f * light)),
                             Theme.S(Theme.RadiusFrame));

        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var glyph = icon.S();
        var size  = ImGui.CalcTextSize(glyph);
        dl.AddText(new Vector2(MathF.Round(position.X + (side - size.X) * 0.5f),
                               MathF.Round(position.Y + (side - size.Y) * 0.5f)),
                   ImGui.GetColorU32(Theme.Text), glyph);

        if (tooltip != null && hovered) Feedback.Tooltip(tooltip);

        return clicked;
    }

    private static void OpenSite() =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(Plugin.Config.BaseUrl) { UseShellExecute = true });
}
