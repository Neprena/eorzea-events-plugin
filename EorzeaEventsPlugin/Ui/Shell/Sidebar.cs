using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using EorzeaEventsPlugin.Ui.Components;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Shell;

/// <summary>
/// Colonne de navigation : entrées regroupées par sections, l'entrée active
/// entourée d'un halo de cristal qui glisse d'une entrée à l'autre.
///
/// La colonne se règle en largeur avec la poignée de son bord droit. Tirée
/// sous un seuil, elle se replie en icônes, chacune avec un petit libellé
/// dessous ; les titres de section deviennent alors de simples traits. Un
/// double-clic sur la poignée replie ou déplie d'un coup.
///
/// Les pictogrammes sont centrés sur une gouttière de largeur fixe, pour que
/// tous les libellés démarrent à la même abscisse quelle que soit la largeur du
/// glyphe.
/// </summary>
internal static class Sidebar
{
    /// <summary>Abscisse du centre des pictogrammes, colonne dépliée.</summary>
    private const float IconGutter = 24f;

    /// <summary>Hauteur d'un titre de section, et d'un trait de section une fois repliée.</summary>
    private const float SectionHeader  = 28f;
    private const float SectionDivider = 13f;

    /// <summary>Largeur de la zone de prise de la poignée.</summary>
    private const float GripWidth = 6f;

    /// <summary>En deçà, la colonne se replie en icônes. À mi-chemin entre les deux largeurs.</summary>
    private const float CollapseThreshold = (Theme.SidebarMin + Theme.SidebarRail) * 0.5f;

    /// <summary>Clés des valeurs animées : largeur et repère.</summary>
    private const uint WidthKey     = 0xEE5D0001;
    private const uint IndicatorY   = 0xEE5D0002;
    private const uint IndicatorH   = 0xEE5D0003;

    /// <summary>Largeur visée pendant qu'on tire la poignée, en pixels à 100 %, ou null.</summary>
    private static float? _dragWidth;

    /// <summary>
    /// Écart entre le pointeur et le bord de la colonne à la prise : la largeur
    /// se déduit ensuite de la position du pointeur, et non de ses déplacements
    /// cumulés, si bien que la poignée reste sous la souris même après l'avoir
    /// tirée au-delà d'une borne.
    /// </summary>
    private static float _dragOffset;

    /// <summary>Largeur affichée, en pixels écran. La coque s'en sert pour placer le contenu.</summary>
    public static float CurrentWidth { get; private set; }

    private readonly record struct Slot(ShellPage Page, float Y, float Height, bool Header, bool Divider);

    /// <summary>Dessine la colonne. Retourne l'identifiant cliqué, ou null.</summary>
    public static string? Draw(IReadOnlyList<ShellPage> pages, string activeId, float height)
    {
        var config  = Plugin.Config;
        var logical = Motion.Approach(WidthKey, TargetWidth(config), Motion.Emphasised);
        var width   = MathF.Round(Theme.S(logical));
        var rail    = logical < CollapseThreshold;

        CurrentWidth = width;

        // Un cran plus opaque que le contenu, comme le cadre d'une vitre.
        using var background = ImRaii.PushColor(ImGuiCol.ChildBg,
                                                Theme.Alpha(Theme.BgSidebar, AppShell.ChromeOpacity(0.04f)));
        using var child = ImRaii.Child("##sidebar", new Vector2(width, height), false,
                                       ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!child) return null;

        var dl  = ImGui.GetWindowDrawList();
        var top = ImGui.GetWindowPos();

        // Séparation avec le contenu : la teinte seule ne suffit pas à marquer
        // la limite quand la page affiche une carte claire juste à côté.
        dl.AddLine(new Vector2(top.X + width - 0.5f, top.Y),
                   new Vector2(top.X + width - 0.5f, top.Y + height),
                   ImGui.GetColorU32(Theme.BorderSoft), 1f);

        var slots = Layout(pages, height, rail);

        DrawIndicator(dl, top, width, slots, activeId);

        string? clicked = null;
        foreach (var slot in slots)
        {
            if (slot.Header)  DrawHeader(dl, top, width, slot);
            if (slot.Divider) DrawDivider(dl, top, width, slot);

            if (DrawItem(dl, top, width, slot, activeId, rail)) clicked = slot.Page.Id;
        }

        DrawGrip(dl, top, width, height, config);

        return clicked;
    }

    // ─── Largeur ──────────────────────────────────────────────────────────────

    private static float TargetWidth(Configuration config)
    {
        if (_dragWidth is { } dragging)
            return dragging < CollapseThreshold
                ? Theme.SidebarRail
                : Math.Clamp(dragging, Theme.SidebarMin, Theme.SidebarMax);

        return config.SidebarCollapsed
            ? Theme.SidebarRail
            : Math.Clamp(config.SidebarWidth, Theme.SidebarMin, Theme.SidebarMax);
    }

    /// <summary>
    /// Poignée du bord droit : une bande à prendre sur toute la hauteur, et un
    /// trait lumineux au milieu qui s'allume au survol.
    /// </summary>
    private static void DrawGrip(ImDrawListPtr dl, Vector2 top, float width, float height, Configuration config)
    {
        var grip = Theme.S(GripWidth);

        ImGui.SetCursorScreenPos(new Vector2(top.X + width - grip, top.Y));
        ImGui.InvisibleButton("##sidebar_grip", new Vector2(grip, height));

        var hovered = ImGui.IsItemHovered();
        var active  = ImGui.IsItemActive();

        if (hovered || active) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

        var pointer = (ImGui.GetIO().MousePos.X - top.X) / ImGuiHelpers.GlobalScale;

        if (ImGui.IsItemActivated())
        {
            _dragWidth  = config.SidebarCollapsed ? Theme.SidebarRail : config.SidebarWidth;
            _dragOffset = width / ImGuiHelpers.GlobalScale - pointer;
        }

        // Le double-clic annule la prise qu'a ouverte son premier clic : sans
        // ça, le relâchement réécrirait la largeur d'avant par-dessus la bascule.
        if (hovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _dragWidth = null;
            config.SidebarCollapsed = !config.SidebarCollapsed;
            config.Save();
        }

        if (active && _dragWidth is not null)
        {
            _dragWidth = pointer + _dragOffset;

            // La colonne suit la souris sans retard : l'animation ne sert
            // qu'aux bascules, pas au geste qu'on fait soi-même.
            Motion.Set(WidthKey, TargetWidth(config));
        }

        if (ImGui.IsItemDeactivated() && _dragWidth is { } released)
        {
            _dragWidth = null;

            var collapsed = released < CollapseThreshold;
            var chosen    = collapsed ? config.SidebarWidth : Math.Clamp(released, Theme.SidebarMin, Theme.SidebarMax);

            // Un simple clic sur la poignée ne réécrit pas la configuration.
            if (collapsed != config.SidebarCollapsed || MathF.Abs(chosen - config.SidebarWidth) >= 0.5f)
            {
                config.SidebarCollapsed = collapsed;
                config.SidebarWidth     = chosen;
                config.Save();
            }
        }

        var light = Motion.Approach(ImGui.GetID("##sidebar_grip_light"), hovered || active ? 1f : 0f, Motion.Fast);
        var half  = Theme.S(18f + 8f * light);
        var x     = top.X + width - Theme.S(1.5f);
        var mid   = top.Y + height * 0.5f;

        if (light > 0.01f)
            Surface.Glow(dl, new Vector2(x - Theme.S(1.5f), mid - half), new Vector2(x + Theme.S(1.5f), mid + half),
                         Theme.S(2f), Theme.Alpha(Theme.Accent, light), spread: 5f);

        dl.AddRectFilled(new Vector2(x - Theme.S(1.5f), mid - half), new Vector2(x + Theme.S(1.5f), mid + half),
                         ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0.22f + 0.6f * light)), Theme.S(2f));

        if (hovered && !active) Feedback.Tooltip(Plugin.L.SidebarHandleHint);
    }

    // ─── Disposition ──────────────────────────────────────────────────────────

    /// <summary>
    /// Place toutes les entrées avant d'en dessiner une seule : le repère se
    /// peint sous les libellés, il faut donc connaître la place de l'entrée
    /// active avant de dessiner quoi que ce soit.
    /// </summary>
    private static List<Slot> Layout(IReadOnlyList<ShellPage> pages, float height, bool rail)
    {
        var slots = new List<Slot>(pages.Count);
        var item  = Theme.S(rail ? Theme.SidebarRailItem : Theme.SidebarItem);
        var y     = Theme.S(Theme.GapS);

        string? section = null;
        var first = true;

        foreach (var page in pages)
        {
            if (page.Pinned) continue;

            var current = page.Section?.Invoke();
            var opens   = current != null && current != section;
            section = current;

            var header  = opens && !rail;
            var divider = opens && rail && !first;

            if (header)  y += Theme.S(SectionHeader);
            if (divider) y += Theme.S(SectionDivider);

            slots.Add(new Slot(page, y, item, header, divider));
            y += item;
            first = false;
        }

        // Entrées ancrées en bas de colonne.
        var pinned = pages.Where(p => p.Pinned).ToList();
        var bottom = height - Theme.S(Theme.GapM) - pinned.Count * item;

        foreach (var page in pinned)
        {
            slots.Add(new Slot(page, MathF.Max(y, bottom), item, false, false));
            bottom += item;
            y      += item;
        }

        return slots;
    }

    /// <summary>
    /// Le repère de l'entrée active : un cadre de cristal dans son halo. Sa
    /// position et sa hauteur sont animées, il glisse donc d'une entrée à
    /// l'autre au lieu d'y sauter.
    /// </summary>
    private static void DrawIndicator(ImDrawListPtr dl, Vector2 top, float width, List<Slot> slots, string activeId)
    {
        var index = slots.FindIndex(s => s.Page.Id == activeId);
        if (index < 0) return;

        var slot  = slots[index];
        var y     = Motion.Approach(IndicatorY, slot.Y, Motion.Emphasised);
        var h     = Motion.Approach(IndicatorH, slot.Height, Motion.Emphasised);
        var inset = Theme.S(Theme.GapM);
        var min   = top + new Vector2(inset, y + Theme.S(2f));
        var max   = top + new Vector2(width - inset, y + h - Theme.S(2f));
        var r     = Theme.S(Theme.RadiusCard - 2f);

        Surface.Glow(dl, min, max, r, Theme.Accent, spread: 5f);
        dl.AddRectFilled(min, max, ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0.14f)), r);
        dl.AddRect(min, max, ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0.70f)), r, ImDrawFlags.None, 1f);
    }

    private static void DrawHeader(ImDrawListPtr dl, Vector2 top, float width, Slot slot)
    {
        using var font = Fonts.PushSmall();

        var text = Truncate(slot.Page.Section!.Invoke().ToUpperInvariant(), width - Theme.S(IconGutter + 8f));
        var size = ImGui.CalcTextSize(text);
        var y    = top.Y + slot.Y - Theme.S(SectionHeader) + (Theme.S(SectionHeader) - size.Y) * 0.5f + Theme.S(3f);

        dl.AddText(new Vector2(top.X + Theme.S(IconGutter - 6f), y), ImGui.GetColorU32(Theme.TextFaint), text);
    }

    private static void DrawDivider(ImDrawListPtr dl, Vector2 top, float width, Slot slot)
    {
        var y = MathF.Round(top.Y + slot.Y - Theme.S(SectionDivider) * 0.5f);
        dl.AddLine(new Vector2(top.X + Theme.S(16f), y), new Vector2(top.X + width - Theme.S(16f), y),
                   ImGui.GetColorU32(Theme.BorderSoft), 1f);
    }

    // ─── Entrées ──────────────────────────────────────────────────────────────

    private static bool DrawItem(ImDrawListPtr dl, Vector2 top, float width, Slot slot, string activeId, bool rail)
    {
        var page   = slot.Page;
        var origin = top + new Vector2(0f, slot.Y);

        ImGui.SetCursorScreenPos(origin);
        ImGui.InvisibleButton($"##nav_{page.Id}", new Vector2(width - Theme.S(GripWidth), slot.Height));

        var hovered = ImGui.IsItemHovered();
        var clicked = ImGui.IsItemClicked();
        var active  = page.Id == activeId;

        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Le survol s'allume et s'éteint en fondu. Pas sur l'entrée active,
        // que le repère éclaire déjà.
        var light = Motion.Approach(ImGui.GetID($"##nav_light_{page.Id}"), hovered && !active ? 1f : 0f, Motion.Fast);
        if (light > 0.01f)
        {
            var inset = Theme.S(Theme.GapM);
            dl.AddRectFilled(origin + new Vector2(inset, Theme.S(2f)),
                             origin + new Vector2(width - inset, slot.Height - Theme.S(2f)),
                             ImGui.GetColorU32(Theme.Alpha(Theme.Text, 0.06f * light)),
                             Theme.S(Theme.RadiusCard - 2f));
        }

        var tint  = ImGui.GetColorU32(active || hovered ? Theme.Text : Theme.TextMuted);
        var label = page.Label();
        var glyph = page.Icon.S();
        var count = page.Badge?.Invoke() ?? 0;
        var lock_ = count == 0 && page.Locked?.Invoke() == true;

        if (rail)
        {
            DrawRailItem(dl, origin, width, slot.Height, glyph, label, tint, count, lock_);

            // Libellé abrégé sous l'icône : le nom entier reste au survol.
            if (hovered) Feedback.Tooltip(label);
        }
        else
        {
            DrawDrawerItem(dl, origin, width, slot.Height, glyph, label, tint, count, lock_);
        }

        return clicked;
    }

    private static void DrawDrawerItem(ImDrawListPtr dl, Vector2 origin, float width, float item,
                                       string glyph, string label, uint tint, int count, bool locked)
    {
        // FontAwesome étant fusionné dans la police de corps, ni la mesure ni le
        // tracé ne nécessitent de bascule de police.
        var glyphSize = ImGui.CalcTextSize(glyph);
        var gutter    = Theme.S(IconGutter);
        dl.AddText(origin + new Vector2(gutter - glyphSize.X * 0.5f, (item - glyphSize.Y) * 0.5f), tint, glyph);

        var trailing  = count > 0 || locked ? Theme.S(34f) : Theme.S(10f);
        var labelX    = origin.X + Theme.S(IconGutter + 18f);
        var text      = Truncate(label, origin.X + width - trailing - labelX);
        var labelSize = ImGui.CalcTextSize(text);
        dl.AddText(new Vector2(labelX, origin.Y + (item - labelSize.Y) * 0.5f), tint, text);

        var center = origin + new Vector2(width - Theme.S(22f), item * 0.5f);
        if (count > 0) DrawBadge(dl, center, count);
        else if (locked) DrawLock(dl, center);
    }

    private static void DrawRailItem(ImDrawListPtr dl, Vector2 origin, float width, float item,
                                     string glyph, string label, uint tint, int count, bool locked)
    {
        var glyphSize = ImGui.CalcTextSize(glyph);
        var iconTop   = origin.Y + Theme.S(10f);
        var iconX     = origin.X + (width - glyphSize.X) * 0.5f;
        dl.AddText(new Vector2(iconX, iconTop), tint, glyph);

        using (Fonts.PushSmall())
        {
            var text = Truncate(label, width - Theme.S(14f));
            var size = ImGui.CalcTextSize(text);
            dl.AddText(new Vector2(origin.X + (width - size.X) * 0.5f, origin.Y + item - size.Y - Theme.S(7f)),
                       tint, text);
        }

        // Pastille sur le coin haut droit de l'icône.
        var corner = new Vector2(iconX + glyphSize.X + Theme.S(2f), iconTop + Theme.S(2f));
        if (count > 0) DrawBadge(dl, corner, count);
        else if (locked) DrawLock(dl, corner);
    }

    /// <summary>Pastille de compteur, dans l'or des actions.</summary>
    private static void DrawBadge(ImDrawListPtr dl, Vector2 center, int count)
    {
        var text = count > 99 ? "99+" : count.ToString();

        using var font = Fonts.PushSmall();
        var size   = ImGui.CalcTextSize(text);
        var radius = MathF.Max(Theme.S(8.5f), size.X * 0.5f + Theme.S(4f));
        var half   = new Vector2(radius, Theme.S(8.5f));

        dl.AddRectFilled(center - half, center + half, ImGui.GetColorU32(Theme.Action), Theme.S(8.5f));
        dl.AddText(center - size * 0.5f, ImGui.GetColorU32(Theme.TextOnAction), text);
    }

    /// <summary>
    /// Cadenas discret, à la place de la pastille. L'entrée reste cliquable :
    /// montrer ce qu'on gagne à lier un personnage vaut mieux que le cacher.
    /// </summary>
    private static void DrawLock(ImDrawListPtr dl, Vector2 center)
    {
        using var font = Fonts.PushSmall();
        var glyph = Icons.Locked.S();
        var size  = ImGui.CalcTextSize(glyph);
        dl.AddText(center - size * 0.5f, ImGui.GetColorU32(Theme.TextFaint), glyph);
    }

    /// <summary>Le texte tel quel s'il tient dans la largeur, sinon abrégé par « … ».</summary>
    private static string Truncate(string text, float room)
    {
        if (room <= 0f) return string.Empty;
        if (ImGui.CalcTextSize(text).X <= room) return text;

        const string ellipsis = "…";
        var length = text.Length;
        while (length > 0 && ImGui.CalcTextSize(text[..length] + ellipsis).X > room) length--;

        return length == 0 ? string.Empty : text[..length].TrimEnd() + ellipsis;
    }
}
