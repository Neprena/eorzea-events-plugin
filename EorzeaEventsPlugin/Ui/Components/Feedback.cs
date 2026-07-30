using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Retours à l'utilisateur : infobulles, pastilles de statut, états vides,
/// squelettes de chargement et bandeaux d'alerte.
///
/// Le plugin n'avait aucune infobulle. C'est pourtant ce qui rend le plus de
/// service pour le moins de code : cela permet de raccourcir les libellés
/// sans perdre l'explication.
/// </summary>
internal static class Feedback
{
    // ─── Infobulles ───────────────────────────────────────────────────────────

    /// <summary>Infobulle thématisée, à appeler quand le survol est déjà établi.</summary>
    public static void Tooltip(string text, string? title = null)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Theme.S(10f, 8f));
        using var color = ImRaii.PushColor(ImGuiCol.PopupBg, Theme.BgSidebar);
        using var tip   = ImRaii.Tooltip();

        ImGui.PushTextWrapPos(Theme.S(320f));
        if (title != null)
        {
            Text.H2(title);
            ImGui.Spacing();
        }
        ImGui.TextColored(title != null ? Theme.TextMuted : Theme.Text, text);
        ImGui.PopTextWrapPos();
    }

    /// <summary>Infobulle sur le dernier widget dessiné.</summary>
    public static void TooltipOnHover(string text, string? title = null)
    {
        if (ImGui.IsItemHovered()) Tooltip(text, title);
    }

    /// <summary>Point d'interrogation inline qui révèle une explication.</summary>
    public static void Help(string text)
    {
        Text.Icon(FontAwesomeIcon.QuestionCircle, Theme.TextFaint);
        TooltipOnHover(text);
    }

    // ─── Statut ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Pastille colorée, éventuellement suivie d'un libellé. Le halo pulsé est
    /// réservé aux états actifs et respecte le réglage d'accessibilité
    /// « animations réduites » de Dalamud.
    /// </summary>
    public static void StatusDot(Vector4 color, string? label = null, bool pulse = false)
    {
        var radius = Theme.S(4f);
        var height = ImGui.GetTextLineHeight();
        var p0     = ImGui.GetCursorScreenPos();
        var center = new Vector2(p0.X + radius, p0.Y + height * 0.5f);
        var dl     = ImGui.GetWindowDrawList();

        if (pulse && !Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion)
        {
            var t    = (MathF.Sin((float)ImGui.GetTime() * 2.4f) + 1f) * 0.5f;
            var halo = radius + Theme.S(2f) + t * Theme.S(3f);
            dl.AddCircleFilled(center, halo, ImGui.GetColorU32(Theme.Alpha(color, 0.22f * (1f - t))));
        }

        dl.AddCircleFilled(center, radius, ImGui.GetColorU32(color));
        ImGui.Dummy(new Vector2(radius * 2f, height));

        if (label == null) return;
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        ImGui.TextColored(Theme.TextMuted, label);
    }

    // ─── État vide ────────────────────────────────────────────────────────────

    /// <summary>
    /// Bloc centré affiché à la place d'une liste vide : grande icône estompée,
    /// titre, explication, et appel à l'action facultatif.
    /// </summary>
    public static void EmptyState(FontAwesomeIcon icon, string title,
                                  string? description = null,
                                  string? ctaLabel = null, Action? onCta = null)
    {
        var avail = ImGui.GetContentRegionAvail();
        var block = Theme.S(description != null ? 108f : 78f);

        if (avail.Y > block)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (avail.Y - block) * 0.4f);

        using (Fonts.PushTitle())
            Center(icon.S(), Theme.Alpha(Theme.TextFaint, 0.55f));

        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapM)));

        using (Fonts.PushH2())
            Center(title, Theme.TextMuted);

        if (description != null)
        {
            ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapXs)));
            using var _ = Fonts.PushSmall();
            Center(description, Theme.TextFaint);
        }

        if (ctaLabel == null || onCta == null) return;

        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapL)));
        var width = Btn.Measure(ctaLabel, Icons.Plus);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - width) * 0.5f);
        if (Btn.Draw(ctaLabel, BtnTone.Primary, icon: Icons.Plus)) onCta();
    }

    private static void Center(string text, Vector4 color)
    {
        var width = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - width) * 0.5f);
        ImGui.TextColored(color, text);
    }

    // ─── Chargement ───────────────────────────────────────────────────────────

    /// <summary>Barre grise animée, à la place d'un texte « chargement ».</summary>
    public static void SkeletonLine(float width = -1f, float height = 14f)
    {
        var w  = width > 0f ? Theme.S(width) : ImGui.GetContentRegionAvail().X;
        var h  = Theme.S(height);
        var p0 = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();

        var reduced = Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion;
        var shimmer = reduced ? 0.5f : (MathF.Sin((float)ImGui.GetTime() * 1.8f) + 1f) * 0.5f;
        var tint    = Theme.Mix(Theme.BgSurface, Theme.BgRaised, shimmer);

        dl.AddRectFilled(p0, p0 + new Vector2(w, h), ImGui.GetColorU32(tint), Theme.S(4f));
        ImGui.Dummy(new Vector2(w, h));
    }

    /// <summary>Empreinte de plusieurs cartes en cours de chargement.</summary>
    public static void SkeletonCards(int count = 3)
    {
        for (var i = 0; i < count; i++)
        {
            using var card = Card.Begin($"##skeleton{i}", CardTone.Flat, interactive: false);
            SkeletonLine(160f, 16f);
            ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapXs)));
            SkeletonLine(-1f, 11f);
            ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapXs)));
            SkeletonLine(220f, 11f);
        }
    }

    // ─── Alerte ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Bandeau d'information contextuelle : surface teintée de la couleur
    /// d'état, barre d'accent à gauche, icône, titre, texte et actions.
    /// </summary>
    public static void Alert(Vector4 color, FontAwesomeIcon icon, string title,
                             string? description = null, Action? actions = null)
    {
        using var card = Card.Begin($"##alert{title.GetHashCode()}", CardTone.Flat,
                                    interactive: false,
                                    background: Theme.Mix(Theme.BgSurface, color, 0.16f),
                                    border: Theme.Alpha(color, 0.45f),
                                    accent: color);

        Text.WithIcon(icon, title, color, color);

        if (description != null)
        {
            ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapXs)));
            Text.Wrapped(description, Theme.TextMuted);
        }

        if (actions == null) return;
        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapS)));
        actions();
    }
}
