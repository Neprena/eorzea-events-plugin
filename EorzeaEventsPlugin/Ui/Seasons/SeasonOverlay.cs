using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Ce que les saisons posent par-dessus le contenu de la fenêtre principale :
/// la toile d'araignée, le fantôme de minuit, l'objet caché de la page et le
/// calendrier du moment (Avent, billets doux).
///
/// Même technique que <see cref="Components.Snackbar"/> : des fenêtres enfants
/// créées après le contenu, donc dessinées au-dessus. Celle du décor ne prend
/// aucune entrée, la souris passe au travers ; les autres sont réduites au
/// rectangle de ce qu'on peut cliquer.
/// </summary>
internal static class SeasonOverlay
{
    private const ImGuiWindowFlags Bare =
        ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    public static void Draw(Vector2 min, Vector2 max, string pageId)
    {
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        Front(min, max);
        Hidden(min, max, pageId);
        DrawDaily(min, max);
    }

    // ─── Décor de premier plan ────────────────────────────────────────────────

    private static void Front(Vector2 min, Vector2 max)
    {
        var web       = SeasonState.Decor && SeasonState.Current == Season.Halloween;
        var ghost     = GhostProgress();
        var fireworks = SeasonDecor.FireworksNow;
        if (!web && ghost == null && !fireworks) return;

        ImGui.SetCursorScreenPos(min);
        using var child = ImRaii.Child("##season_front", max - min, false, Bare | ImGuiWindowFlags.NoInputs);
        if (!child) return;

        var dl = ImGui.GetWindowDrawList();

        // La toile part du coin bas droit : l'image a son ancrage en haut à
        // gauche, retournée sur les deux axes.
        if (web)
        {
            var side = Theme.S(112f);
            SeasonArt.Draw(dl, "cobweb", max - new Vector2(side, side), new Vector2(side, side), 0.9f,
                           flipX: true, flipY: true);
        }

        if (ghost is { } progress) Ghost(dl, min, max, progress);

        if (fireworks) SeasonDecor.Fireworks(dl, min, max);
    }

    /// <summary>
    /// Avancement dans l'heure de minuit d'Eorzea, ou null hors de cette heure.
    /// Une heure d'Eorzea dure 175 secondes, une journée 70 minutes : le
    /// fantôme passe une fois toutes les 70 minutes et reste un peu moins de
    /// trois minutes.
    /// </summary>
    private static float? GhostProgress()
    {
        if (!SeasonState.EasterEggs || SeasonState.Current != Season.Halloween) return null;

#if DEBUG
        if (SeasonState.ForceGhost) return (float)(ImGui.GetTime() % 20.0 / 20.0);
#endif

        var eorzeaMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 3600L / 175L;
        if (eorzeaMs / 3_600_000L % 24L != 0L) return null;

        return eorzeaMs % 3_600_000L / 3_600_000f;
    }

    /// <summary>Le fantôme traverse la page de droite à gauche, en flottant, et salue au milieu.</summary>
    private static void Ghost(ImDrawListPtr dl, Vector2 min, Vector2 max, float progress)
    {
        var t     = SeasonState.Time;
        var size  = Theme.S(56f, 64f);
        var width = max.X - min.X;
        var x     = max.X + size.X - progress * (width + size.X * 2f);
        var y     = min.Y + (max.Y - min.Y) * 0.38f + MathF.Sin(t * 1.3f) * Theme.S(8f);
        var fade  = Math.Clamp(MathF.Min(progress, 1f - progress) * 10f, 0f, 1f);

        SeasonArt.Centered(dl, "ghost", new Vector2(x, y), size, 0.92f * fade);

        if (progress is < 0.3f or > 0.7f) return;

        var text   = Plugin.L.GhostBubble;
        var label  = ImGui.CalcTextSize(text);
        var pad    = Theme.S(10f, 5f);
        var bubble = new Vector2(label.X, label.Y) + pad * 2f;
        var at     = new Vector2(x - size.X * 0.5f - Theme.S(8f) - bubble.X, y - size.Y * 0.35f);

        Surface.Panel(dl, at, at + bubble, Theme.BgSurface, Theme.BorderLight, Theme.S(10f), shadowSpread: 6f);
        dl.AddText(at + pad, ImGui.GetColorU32(Theme.Text), text);
    }

    // ─── Objet caché ──────────────────────────────────────────────────────────

    /// <summary>
    /// La citrouille ou l'œuf de la page, posé au bas du contenu à une abscisse
    /// propre à la page. Un clic le ramasse ; il ne revient plus de l'année.
    /// </summary>
    private static void Hidden(Vector2 min, Vector2 max, string pageId)
    {
        if (SeasonHunts.Active is not { } hunt || Array.IndexOf(hunt.Pages, pageId) < 0) return;
        if (SeasonHunts.Has(hunt.Key, pageId)) return;

        var size = hunt == SeasonHunts.Pumpkins ? Theme.S(30f, 30f) : Theme.S(22f, 28f);
        var x    = MathF.Round(min.X + (max.X - min.X - size.X) * SeasonHunts.Spot(hunt, pageId));
        var y    = MathF.Round(max.Y - size.Y - Theme.S(6f));

        ImGui.SetCursorScreenPos(new Vector2(x, y));
        using var child = ImRaii.Child("##season_hidden", size, false, Bare);
        if (!child) return;

        if (ImGui.InvisibleButton("##grab", size)) SeasonHunts.Collect(hunt, pageId);

        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Il respire doucement, et grossit un peu sous la souris.
        var t     = SeasonState.Time;
        var scale = (hovered ? 1.12f : 1f) + MathF.Sin(t * 1.6f) * 0.03f;
        SeasonArt.Rotated(ImGui.GetWindowDrawList(), SeasonHunts.ArtFor(hunt, pageId), new Vector2(x, y) + size * 0.5f,
                          size * scale, MathF.Sin(t * 0.8f) * 0.08f);
    }

    // ─── Calendriers ──────────────────────────────────────────────────────────

    /// <summary>Le calendrier ouvert, et le jour du mois affiché.</summary>
    private static SeasonDaily? _open;
    private static int          _day;

    /// <summary>La case du jour n'est pas encore ouverte : le bouton de la barre porte une pastille.</summary>
    public static bool DailyUnread =>
        SeasonHunts.ActiveDaily is { } daily && !SeasonHunts.Opened(daily, SeasonState.Now.Day);

    public static void ToggleDaily()
    {
        _open = _open == null ? SeasonHunts.ActiveDaily : null;
        if (_open == null) return;

        _day = SeasonState.Now.Day;
        SeasonHunts.Open(_open, _day);
    }

    private static void DrawDaily(Vector2 min, Vector2 max)
    {
        if (_open is not { } daily) return;
        if (SeasonHunts.ActiveDaily != daily)
        {
            _open = null;
            return;
        }

        var l       = Plugin.L;
        var today   = SeasonState.Now.Day;
        var width   = MathF.Min(Theme.S(340f), max.X - min.X - Theme.S(24f));
        var pad     = Theme.S(14f);
        var art     = Theme.S(44f);
        var cell    = Theme.S(28f);
        var gap     = Theme.S(5f);
        var inner   = width - pad * 2f;
        var rows    = (daily.Count + 7) / 8;
        var prompts = daily.Prompts(l);
        var text    = prompts[Math.Clamp(_day - daily.FirstDay, 0, prompts.Length - 1)];
        var hint    = daily.Hint(l);

        float hintHeight;
        using (Fonts.PushSmall())
            hintHeight = ImGui.CalcTextSize(hint, false, inner).Y;

        var textHeight = ImGui.CalcTextSize(text, false, inner).Y;
        var gridHeight = cell * rows + gap * (rows - 1);
        var height     = pad + art + Theme.S(8f) + textHeight + Theme.S(12f) + gridHeight + Theme.S(10f) + hintHeight + pad;

        var origin = new Vector2(max.X - width - Theme.S(12f), min.Y + Theme.S(10f));
        ImGui.SetCursorScreenPos(origin);
        using var child = ImRaii.Child("##season_daily", new Vector2(width, height), false, Bare);
        if (!child) return;

        var dl = ImGui.GetWindowDrawList();

        // Opaque, comme une notification : posé sur une carte, le texte du
        // dessous ne doit pas transparaître.
        Surface.Panel(dl, origin, origin + new Vector2(width, height), Theme.BgSurface, Theme.BorderLight,
                      shadowSpread: 10f);

        // En-tête : surtitre, jour, et l'image du calendrier à droite.
        SeasonArt.Draw(dl, daily.Art, new Vector2(origin.X + width - pad - art, origin.Y + pad - Theme.S(2f)),
                       new Vector2(art, art));

        var y = origin.Y + pad;
        using (Fonts.PushSmall())
        {
            dl.AddText(new Vector2(origin.X + pad, y), ImGui.GetColorU32(Theme.Accent), daily.Title(l).ToUpper(l.Culture));
            y += ImGui.GetTextLineHeight() + Theme.S(2f);
        }

        using (Fonts.PushH2())
            dl.AddText(new Vector2(origin.X + pad, y), ImGui.GetColorU32(Theme.TitleGold),
                       string.Format(daily.DayTitle(l), _day));

        y = origin.Y + pad + art + Theme.S(8f);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, y));
        ImGui.PushTextWrapPos(width - pad);
        ImGui.TextColored(Theme.Text, text);
        ImGui.PopTextWrapPos();

        // Les cases, huit par rangée : ouvertes en accent, celle du jour dans
        // la couleur de l'action, l'oubliée cerclée, les suivantes éteintes et
        // inertes.
        y += textHeight + Theme.S(12f);
        var step = (inner - gap * 7f) / 8f;

        using (Fonts.PushSmall())
            for (var i = 0; i < daily.Count; i++)
            {
                var day  = daily.FirstDay + i;
                var cMin = new Vector2(origin.X + pad + i % 8 * (step + gap), y + i / 8 * (cell + gap));
                var cMax = cMin + new Vector2(step, cell);
                var r    = Theme.S(7f);

                var reachable = day <= today;
                var opened    = SeasonHunts.Opened(daily, day);

                ImGui.SetCursorScreenPos(cMin);
                if (ImGui.InvisibleButton($"##daily{day}", cMax - cMin) && reachable)
                {
                    _day = day;
                    SeasonHunts.Open(daily, day);
                }

                if (reachable && ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                Vector4 ink;
                if (day == today)
                {
                    dl.AddRectFilled(cMin, cMax, ImGui.GetColorU32(Theme.Action), r);
                    ink = Theme.TextOnAction;
                }
                else if (!reachable)
                {
                    dl.AddRectFilled(cMin, cMax, ImGui.GetColorU32(Theme.Alpha(Theme.Text, 0.04f)), r);
                    ink = Theme.TextFaint;
                }
                else if (opened)
                {
                    dl.AddRectFilled(cMin, cMax, ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0.16f)), r);
                    ink = Theme.Accent;
                }
                else
                {
                    dl.AddRect(cMin, cMax, ImGui.GetColorU32(Theme.BorderLight), r, ImDrawFlags.None, 1f);
                    ink = Theme.TextMuted;
                }

                if (day == _day && day != today)
                    dl.AddRect(cMin, cMax, ImGui.GetColorU32(Theme.Accent), r, ImDrawFlags.None, Theme.S(1.5f));

                var label = day.ToString();
                var size  = ImGui.CalcTextSize(label);
                dl.AddText(cMin + (cMax - cMin - size) * 0.5f, ImGui.GetColorU32(ink), label);
            }

        y += gridHeight + Theme.S(10f);
        using (Fonts.PushSmall())
        {
            ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, y));
            ImGui.PushTextWrapPos(width - pad);
            ImGui.TextColored(Theme.TextFaint, hint);
            ImGui.PopTextWrapPos();
        }
    }
}
