using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Mise en page : en-têtes de section, séparateurs, alignements et pastilles
/// d'initiales.
/// </summary>
internal static class Layout
{
    // ─── En-tête de section ───────────────────────────────────────────────────

    /// <summary>
    /// Titre de section avec icône, compteur facultatif, filet qui occupe
    /// l'espace restant, et zone d'action alignée à droite.
    /// </summary>
    public static void SectionHeader(string title,
                                     FontAwesomeIcon? icon = null,
                                     int? count = null,
                                     Vector4? tone = null,
                                     Action? actions = null,
                                     float actionsWidth = 0f)
    {
        var color = tone ?? Theme.Accent;

        if (icon is { } value)
        {
            Text.Icon(value, color);
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
        }

        using (Fonts.PushH2())
            ImGui.TextColored(color, title);

        if (count is { } total)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapS));
            Chip.Draw(total.ToString(), ChipTone.Neutral);
        }

        // Filet horizontal entre le titre et les actions.
        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        var start = ImGui.GetCursorScreenPos();
        var span  = ImGui.GetContentRegionAvail().X - actionsWidth - Theme.S(Theme.GapM);
        if (span > Theme.S(8f))
        {
            var y = start.Y + ImGui.GetTextLineHeight() * 0.55f;
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(start.X, y),
                new Vector2(start.X + span, y),
                ImGui.GetColorU32(Theme.BorderSoft),
                Theme.S(1f));
        }

        if (actions != null)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapM));
            RightAlign(actionsWidth);
            actions();
        }
        else
        {
            ImGui.Dummy(Vector2.Zero);
        }

        ImGui.Dummy(new Vector2(0f, Theme.S(Theme.GapS)));
    }

    // ─── Séparateurs ──────────────────────────────────────────────────────────

    public static void Divider(float marginY = Theme.GapM)
    {
        ImGui.Dummy(new Vector2(0f, Theme.S(marginY)));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0f, Theme.S(marginY)));
    }

    public static void Spacer(float height = Theme.GapM) =>
        ImGui.Dummy(new Vector2(0f, Theme.S(height)));

    // ─── Alignements ──────────────────────────────────────────────────────────

    /// <summary>
    /// Place le curseur pour qu'un élément de cette largeur finisse au bord
    /// droit utile, marge de carte déduite.
    /// </summary>
    public static void RightAlign(float itemWidth) =>
        ImGui.SetCursorPosX(ImGui.GetCursorPosX()
                            + Math.Max(0f, ImGui.GetContentRegionAvail().X
                                           - Card.RightInset - itemWidth));

    /// <summary>Place le curseur pour centrer horizontalement un élément.</summary>
    public static void Center(float itemWidth) =>
        ImGui.SetCursorPosX(ImGui.GetCursorPosX()
                            + Math.Max(0f, (ImGui.GetContentRegionAvail().X - itemWidth) * 0.5f));

    // ─── Pastille d'initiales ─────────────────────────────────────────────────

    /// <summary>
    /// Pastille colorée portant les initiales d'un personnage. Aucune image
    /// n'est téléchargée : la couleur est dérivée du nom, donc stable et
    /// reconnaissable d'une session à l'autre.
    /// </summary>
    public static void Avatar(string name, float size = 32f, Vector4? status = null)
    {
        var side   = Theme.S(size);
        var origin = ImGui.GetCursorScreenPos();
        var center = origin + new Vector2(side * 0.5f, side * 0.5f);
        var dl     = ImGui.GetWindowDrawList();

        var background = Theme.FromName(name);
        dl.AddCircleFilled(center, side * 0.5f, ImGui.GetColorU32(background));

        var initials = Initials(name);
        using (Fonts.PushSmall())
        {
            var textSize = ImGui.CalcTextSize(initials);
            dl.AddText(center - textSize * 0.5f,
                ImGui.GetColorU32(Theme.TextOn(background)), initials);
        }

        if (status is { } dot)
        {
            var radius = Theme.S(size * 0.17f);
            var edge   = center + new Vector2(side * 0.35f, side * 0.35f);
            dl.AddCircleFilled(edge, radius + Theme.S(1.5f), ImGui.GetColorU32(Theme.BgSurface));
            dl.AddCircleFilled(edge, radius, ImGui.GetColorU32(dot));
        }

        ImGui.Dummy(new Vector2(side, side));
    }

    /// <summary>« Leera Rajani » donne « LR », « Neprena » donne « NE ».</summary>
    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1)
            return parts[0].Length >= 2
                ? parts[0][..2].ToUpperInvariant()
                : parts[0].ToUpperInvariant();

        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }
}
