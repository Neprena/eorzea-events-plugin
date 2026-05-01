using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using System;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

internal static class UiPrimitives
{
    // ── Card ──────────────────────────────────────────────────────────────────
    // Canal 0 = fond (rendu en premier), canal 1 = contenu (au-dessus).
    public static void DrawCard(Action content, Vector4? bgColor = null)
    {
        var dl    = ImGui.GetWindowDrawList();
        var avail = ImGui.GetContentRegionAvail().X;
        var p0    = ImGui.GetCursorScreenPos();

        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1);

        ImGui.Dummy(new Vector2(0, UiStyle.CardPadV));
        ImGui.Indent(UiStyle.CardPadH);

        try
        {
            content();
        }
        finally
        {
            ImGui.Unindent(UiStyle.CardPadH);
            ImGui.Dummy(new Vector2(0, UiStyle.CardPadV));

            var p1 = ImGui.GetCursorScreenPos();
            dl.ChannelsSetCurrent(0);

            var bg = bgColor ?? UiStyle.CardBg;
            dl.AddRectFilled(p0, new Vector2(p0.X + avail, p1.Y),
                ImGui.GetColorU32(bg), UiStyle.CardRounding);
            dl.AddRect(p0, new Vector2(p0.X + avail, p1.Y),
                ImGui.GetColorU32(UiStyle.CardBorder), UiStyle.CardRounding);

            dl.ChannelsMerge();
        }
    }

    // ── Card avec bannière hero (pleine largeur) ──────────────────────────────
    // bannerWrap = null → comportement identique à DrawCard.
    // 3 canaux : 0=fond carte, 1=bannière image, 2=contenu texte/boutons.
    public static void DrawCardWithBanner(IDalamudTextureWrap? bannerWrap, Action content, Vector4? bgColor = null)
    {
        if (bannerWrap == null) { DrawCard(content, bgColor); return; }

        var dl    = ImGui.GetWindowDrawList();
        var avail = ImGui.GetContentRegionAvail().X;
        var p0    = ImGui.GetCursorScreenPos();

        // Cover mode : hauteur fixe, crop centré pour préserver le ratio (comme object-fit: cover)
        var bannerH    = UiStyle.EstabBannerHeight;
        var imgAspect  = bannerWrap.Width / (float)bannerWrap.Height;
        var cardAspect = avail / bannerH;

        float u0, v0, u1, v1;
        if (imgAspect >= cardAspect)
        {
            // Image plus large que la card → crop les côtés, hauteur pleine
            var uRange = cardAspect / imgAspect;
            u0 = (1f - uRange) / 2f; u1 = 1f - u0;
            v0 = 0f; v1 = 1f;
        }
        else
        {
            // Image plus haute que la card → crop haut/bas, largeur pleine
            var vRange = imgAspect / cardAspect;
            v0 = (1f - vRange) / 2f; v1 = 1f - v0;
            u0 = 0f; u1 = 1f;
        }

        dl.ChannelsSplit(3);
        dl.ChannelsSetCurrent(2);

        ImGui.Dummy(new Vector2(0, bannerH));
        ImGui.Dummy(new Vector2(0, UiStyle.CardPadV));
        ImGui.Indent(UiStyle.CardPadH);

        try { content(); }
        finally
        {
            ImGui.Unindent(UiStyle.CardPadH);
            ImGui.Dummy(new Vector2(0, UiStyle.CardPadV));
            var p1 = ImGui.GetCursorScreenPos();

            dl.ChannelsSetCurrent(0);
            var bg = bgColor ?? UiStyle.CardBg;
            dl.AddRectFilled(p0, new Vector2(p0.X + avail, p1.Y),
                ImGui.GetColorU32(bg), UiStyle.CardRounding);
            dl.AddRect(p0, new Vector2(p0.X + avail, p1.Y),
                ImGui.GetColorU32(UiStyle.CardBorder), UiStyle.CardRounding);

            dl.ChannelsSetCurrent(1);
            var bannerEnd = new Vector2(p0.X + avail, p0.Y + bannerH);
            dl.AddImageRounded(bannerWrap.Handle, p0, bannerEnd,
                new Vector2(u0, v0), new Vector2(u1, v1),
                ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)),
                UiStyle.CardRounding,
                ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersTopRight);

            dl.ChannelsMerge();
        }
    }

    // ── Chip ──────────────────────────────────────────────────────────────────
    // Badge non-interactif avec fond arrondi.
    // Utiliser ImGui.SameLine(0, 4) pour enchaîner plusieurs chips.
    public static void DrawChip(string text, Vector4? bgColor = null)
    {
        var bg       = bgColor ?? UiStyle.ChipBg;
        var textSize = ImGui.CalcTextSize(text);
        var chipW    = textSize.X + UiStyle.ChipPadH * 2;
        var chipH    = textSize.Y + UiStyle.ChipPadV * 2;
        var pos      = ImGui.GetCursorScreenPos();
        var dl       = ImGui.GetWindowDrawList();

        dl.AddRectFilled(pos, pos + new Vector2(chipW, chipH),
            ImGui.GetColorU32(bg), UiStyle.ChipRounding);
        dl.AddText(pos + new Vector2(UiStyle.ChipPadH, UiStyle.ChipPadV),
            ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.90f)), text);

        ImGui.Dummy(new Vector2(chipW, chipH));
    }

    // ── Icon inline ───────────────────────────────────────────────────────────
    public static void DrawIcon(string glyph, Vector4? color = null)
    {
        using var _ = Plugin.PluginInterface.UiBuilder.IconFontHandle.Push();
        ImGui.TextColored(color ?? UiStyle.TextMuted, glyph);
    }

    // ── Bannière d'alerte (fond semi-transparent + bordure colorée) ───────────
    public static void DrawAlert(Vector4 color, string title, string desc, Action buttons)
    {
        var dl    = ImGui.GetWindowDrawList();
        var avail = ImGui.GetContentRegionAvail().X;
        var p0    = ImGui.GetCursorScreenPos();

        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1);

        ImGui.Spacing();
        ImGui.Indent(8f);
        ImGui.TextColored(color, title);
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(new Vector4(1f, 1f, 1f, 0.85f), desc);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        buttons();
        ImGui.Unindent(8f);
        ImGui.Spacing();

        var p1 = ImGui.GetCursorScreenPos();
        dl.ChannelsSetCurrent(0);
        dl.AddRectFilled(p0, new Vector2(p0.X + avail, p1.Y),
            ImGui.GetColorU32(new Vector4(color.X * 0.6f, color.Y * 0.6f, color.Z * 0.6f, 0.12f)), 4f);
        dl.AddRect(p0, new Vector2(p0.X + avail, p1.Y),
            ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.50f)), 4f);
        dl.ChannelsMerge();

        ImGui.Spacing();
    }

    // ── Bouton coloré ─────────────────────────────────────────────────────────
    public static bool ColorButton(string label, Vector2 size,
        Vector4 normal, Vector4 hovered, Vector4 active)
    {
        ImGui.PushStyleColor(ImGuiCol.Button,        normal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  active);
        var clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }
}
