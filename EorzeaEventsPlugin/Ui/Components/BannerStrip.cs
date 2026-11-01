using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Bandeau d'une ligne posé en tête de fenêtre : libellé coloré, titre, un
/// bouton et la croix de fermeture. Sert à l'actualité épinglée et au concours
/// mis en avant.
///
/// En carte, avec accroche et boutons, un tel bandeau occupait le haut de
/// chaque page ; le détail se lit dans sa fenêtre. Toute la ligne l'ouvre.
/// </summary>
internal static class BannerStrip
{
    /// <summary>Dessine le bandeau.</summary>
    /// <param name="id">Identifiant ImGui, unique parmi les bandeaux.</param>
    /// <param name="dismissed">Vrai si la croix vient d'être cliquée.</param>
    /// <returns>Vrai si la ligne ou le bouton a été cliqué (jamais pour la croix).</returns>
    public static bool Draw(string id, FontAwesomeIcon kickerIcon, string kicker, Vector4 color,
                            string title, string buttonLabel, FontAwesomeIcon buttonIcon,
                            out bool dismissed)
    {
        var l      = Plugin.L;
        var origin = ImGui.GetCursorScreenPos();
        var width  = ImGui.GetContentRegionAvail().X - Card.RightInset;
        var frame  = ImGui.GetFrameHeight();
        // Assez d'air autour des boutons pour qu'ils ne touchent pas le cadre :
        // à 3 px, le bouton semblait posé en travers du bord.
        var padY   = Theme.S(6f);
        var height = frame + padY * 2f;
        var padX   = Theme.S(Theme.GapL);
        var gap    = Theme.S(Theme.GapS);
        var dl     = ImGui.GetWindowDrawList();
        var radius = Theme.S(Theme.RadiusFrame);

        // Boutons à droite, mesurés d'abord : le titre prend ce qui reste.
        var btnW   = Btn.Measure(buttonLabel, BtnSize.Small, buttonIcon);
        var closeX = origin.X + width - padX - frame;
        var btnX   = closeX - gap - btnW;
        var y      = origin.Y + padY;

        // Zone cliquable : la partie texte seulement, les boutons gardent la leur.
        ImGui.SetCursorScreenPos(origin);
        var open    = ImGui.InvisibleButton($"##{id}strip", new Vector2(Math.Max(1f, btnX - gap - origin.X), height));
        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var tint = Theme.Mix(Theme.BgSurface, color, hovered ? 0.20f : 0.12f);
        dl.AddRectFilled(origin, origin + new Vector2(width, height), ImGui.GetColorU32(tint), radius);
        dl.AddRect(origin, origin + new Vector2(width, height),
                   ImGui.GetColorU32(Theme.Alpha(color, 0.45f)), radius);
        dl.AddRectFilled(origin, origin + new Vector2(Theme.S(3f), height),
                         ImGui.GetColorU32(color), radius, ImDrawFlags.RoundCornersLeft);

        // Libellé coloré, en petit, centré sur la hauteur de la ligne.
        float x = origin.X + padX;
        using (Fonts.PushSmall())
        {
            var label = $"{kickerIcon.S()}  {kicker.ToUpper(l.Culture)}";
            var ky    = origin.Y + (height - ImGui.GetTextLineHeight()) * 0.5f;
            dl.AddText(new Vector2(x, ky), ImGui.GetColorU32(color), label);
            x += ImGui.CalcTextSize(label).X + Theme.S(Theme.GapM);
        }

        using (Fonts.PushBodyStrong())
        {
            var room = btnX - gap - x;
            var text = Ellipsize(Glyphs.Safe(title), room);
            var ty   = origin.Y + (height - ImGui.GetTextLineHeight()) * 0.5f;
            dl.AddText(new Vector2(x, ty), ImGui.GetColorU32(Theme.Text), text);
        }

        ImGui.SetCursorScreenPos(new Vector2(btnX, y));
        if (Btn.Draw(buttonLabel, BtnTone.Primary, BtnSize.Small, buttonIcon, id: $"read{id}")) open = true;

        dismissed = false;
        ImGui.SetCursorScreenPos(new Vector2(closeX, y));
        if (Btn.Icon(Icons.Close, $"dismiss{id}", BtnTone.Ghost, l.BannerDismiss))
        {
            dismissed = true;
            open      = false;
        }

        // Le curseur repart sous la ligne ; le Dummy valide la position.
        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
        ImGui.Dummy(Vector2.Zero);

        return open;
    }

    /// <summary>Coupe le texte avec « … » pour qu'il tienne dans la largeur donnée.</summary>
    internal static string Ellipsize(string text, float limit)
    {
        if (limit <= 0f) return string.Empty;
        if (ImGui.CalcTextSize(text).X <= limit) return text;

        const string dots = "…";
        var end = text.Length;
        while (end > 0 && ImGui.CalcTextSize(text[..end].TrimEnd() + dots).X > limit) end--;
        return end == 0 ? string.Empty : text[..end].TrimEnd() + dots;
    }
}
