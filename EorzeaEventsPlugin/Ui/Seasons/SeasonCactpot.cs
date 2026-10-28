using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Le Mini Cactpot du jour, au festival du Gold Saucer : la grille de neuf
/// cases du jeu, un chiffre déjà visible, trois cases à gratter, puis une
/// ligne à choisir. La somme de la ligne tire la chance du jour, un défi RP.
///
/// Un ticket par jour. La grille se tire de la date, la même toute la journée
/// et d'une session à l'autre ; les cases grattées et la ligne choisie tiennent
/// dans <see cref="Configuration.SeasonalFinds"/>, comme les cases de l'Avent.
/// </summary>
internal static class SeasonCactpot
{
    private const ImGuiWindowFlags Bare =
        ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    /// <summary>Les huit lignes du jeu : trois rangées, trois colonnes, deux diagonales.</summary>
    private static readonly int[][] Lines =
        [[0, 1, 2], [3, 4, 5], [6, 7, 8], [0, 3, 6], [1, 4, 7], [2, 5, 8], [0, 4, 8], [2, 4, 6]];

    private static bool _open;

    public static bool Active => SeasonState.EasterEggs && SeasonState.Current == Season.GoldSaucer;

    private static string Key => $"cactpot-{SeasonState.Now.Year}";

    private static string Day => SeasonState.Now.ToString("MMdd");

    private static bool Scratched(int cell) => SeasonHunts.Has(Key, $"{Day}:c{cell}");

    private static int? ChosenLine()
    {
        for (var line = 0; line < Lines.Length; line++)
            if (SeasonHunts.Has(Key, $"{Day}:l{line}")) return line;
        return null;
    }

    /// <summary>Le ticket du jour est joué : le bouton de la barre perd sa pastille.</summary>
    public static bool Played => ChosenLine() != null;

    public static void Toggle() => _open = !_open;

    private static DateTime                   _gridDate;
    private static (int[] Numbers, int Shown) _grid;

    /// <summary>Les neuf chiffres du jour, et la case montrée d'office. Tirés de la date seule, une fois par jour.</summary>
    private static (int[] Numbers, int Shown) Grid()
    {
        var today = SeasonState.Now.Date;
        if (today == _gridDate) return _grid;

        var random  = new Random(today.Year * 10000 + today.Month * 100 + today.Day);
        var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        random.Shuffle(numbers);

        _gridDate = today;
        _grid     = (numbers, random.Next(numbers.Length));
        return _grid;
    }

    public static void Draw(Vector2 min, Vector2 max)
    {
        if (!_open) return;
        if (!Active)
        {
            _open = false;
            return;
        }

        var l       = Plugin.L;
        var (numbers, shown) = Grid();
        var line    = ChosenLine();
        var done    = line != null;
        var count   = 0;
        for (var i = 0; i < 9; i++)
            if (i != shown && Scratched(i)) count++;
        var picking = count >= 3 && !done;

        var width  = MathF.Min(Theme.S(318f), max.X - min.X - Theme.S(24f));
        var pad    = Theme.S(14f);
        var art    = Theme.S(40f);
        var inner  = width - pad * 2f;
        var cell   = Theme.S(52f);
        var arrow  = Theme.S(26f);
        var gap    = Theme.S(6f);
        var grid   = new Vector2(arrow * 2f + cell * 3f + gap * 4f, arrow + cell * 3f + gap * 3f);
        var hint   = l.CactpotHint;
        var answer = done ? l.CactpotFortunes[Sum(numbers, line!.Value) % l.CactpotFortunes.Length] : null;

        float hintHeight;
        using (Fonts.PushSmall())
            hintHeight = ImGui.CalcTextSize(hint, false, inner).Y;

        var answerHeight = answer == null ? 0f : ImGui.CalcTextSize(answer, false, inner - Theme.S(24f)).Y + Theme.S(32f);
        var height = pad + art + Theme.S(10f) + grid.Y + answerHeight + Theme.S(10f) + hintHeight + pad;

        var origin = new Vector2(max.X - width - Theme.S(12f), min.Y + Theme.S(10f));
        ImGui.SetCursorScreenPos(origin);
        using var child = ImRaii.Child("##season_cactpot", new Vector2(width, height), false, Bare);
        if (!child) return;

        var dl = ImGui.GetWindowDrawList();
        Surface.Panel(dl, origin, origin + new Vector2(width, height), Theme.BgSurface, Theme.BorderLight, shadowSpread: 10f);

        // En-tête : surtitre, consigne du moment, et le jeton à droite.
        SeasonArt.Draw(dl, "coin", new Vector2(origin.X + width - pad - art, origin.Y + pad), new Vector2(art, art));

        var y = origin.Y + pad;
        using (Fonts.PushSmall())
        {
            dl.AddText(new Vector2(origin.X + pad, y), ImGui.GetColorU32(Theme.Accent), l.CactpotTitle.ToUpper(l.Culture));
            y += ImGui.GetTextLineHeight() + Theme.S(2f);
        }

        var title = done ? string.Format(l.CactpotLine, string.Join(" + ", Lines[line!.Value].Select(i => numbers[i])),
                                         Sum(numbers, line.Value))
                  : picking ? l.CactpotPickLine : l.CactpotScratch;
        using (Fonts.PushH2())
            dl.AddText(new Vector2(origin.X + pad, y), ImGui.GetColorU32(Theme.TitleGold), title);

        // La grille : une rangée de flèches en tête (diagonale, trois colonnes,
        // diagonale), puis trois rangées précédées chacune de sa flèche.
        var gridMin = new Vector2(origin.X + (width - grid.X) * 0.5f, origin.Y + pad + art + Theme.S(10f));
        int? hovered = null;

        for (var k = 0; k < Lines.Length; k++)
        {
            var (at, size, dir) = ArrowSlot(k, gridMin, cell, arrow, gap);
            ImGui.SetCursorScreenPos(at);
            if (ImGui.InvisibleButton($"##cactpot_line{k}", size) && picking)
                SeasonHunts.Add(Key, $"{Day}:l{k}");

            var over = picking && ImGui.IsItemHovered();
            if (over)
            {
                hovered = k;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            var chosen = line == k;
            if (chosen || over)
                dl.AddRectFilled(at, at + size, ImGui.GetColorU32(Theme.Alpha(Theme.Action, chosen ? 0.22f : 0.12f)), Theme.S(7f));

            var ink = chosen ? Theme.TitleGold : picking ? Theme.Text : Theme.TextFaint;
            Arrow(dl, at + size * 0.5f, dir, ImGui.GetColorU32(ink));
        }

        using (Fonts.PushH2())
            for (var i = 0; i < 9; i++)
            {
                var cMin   = gridMin + new Vector2(arrow + gap + i % 3 * (cell + gap), arrow + gap + i / 3 * (cell + gap));
                var cMax   = cMin + new Vector2(cell, cell);
                var r      = Theme.S(10f);
                var open   = i == shown || Scratched(i) || done;
                var inLine = line is { } chosen && Lines[chosen].Contains(i);
                var inHover = hovered is { } over && Lines[over].Contains(i);

                ImGui.SetCursorScreenPos(cMin);
                if (ImGui.InvisibleButton($"##cactpot_cell{i}", cMax - cMin) && !open && count < 3)
                    SeasonHunts.Add(Key, $"{Day}:c{i}");
                if (!open && count < 3 && ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                string label;
                Vector4 ink;
                if (open)
                {
                    dl.AddRectFilled(cMin, cMax, ImGui.GetColorU32(Theme.Alpha(Theme.Text, 0.08f)), r);
                    label = numbers[i].ToString();
                    ink   = Theme.Text;
                }
                else
                {
                    // Le dos doré d'une case à gratter.
                    dl.AddRectFilled(cMin, cMax, ImGui.GetColorU32(Theme.Hex(0xF2C24E)), r);
                    label = "?";
                    ink   = Theme.Hex(0x5A3A00);
                }

                if (inLine || inHover)
                    dl.AddRect(cMin, cMax, ImGui.GetColorU32(inLine ? Theme.Action : Theme.Accent), r, ImDrawFlags.None,
                               Theme.S(2f));

                var text = ImGui.CalcTextSize(label);
                dl.AddText(cMin + (cMax - cMin - text) * 0.5f, ImGui.GetColorU32(ink), label);
            }

        y = gridMin.Y + grid.Y;

        // La chance du jour, sur la couleur de l'action.
        if (answer != null)
        {
            y += Theme.S(12f);
            var boxMin = new Vector2(origin.X + pad, y);
            var boxMax = new Vector2(origin.X + width - pad, y + answerHeight - Theme.S(12f));
            dl.AddRectFilled(boxMin, boxMax, ImGui.GetColorU32(Theme.Alpha(Theme.Action, 0.12f)), Theme.S(10f));
            dl.AddRect(boxMin, boxMax, ImGui.GetColorU32(Theme.Border), Theme.S(10f), ImDrawFlags.None, 1f);

            ImGui.SetCursorScreenPos(boxMin + Theme.S(12f, 10f));
            ImGui.PushTextWrapPos(boxMax.X - origin.X - Theme.S(12f));
            ImGui.TextColored(Theme.Text, answer);
            ImGui.PopTextWrapPos();
            y = boxMax.Y;
        }

        y += Theme.S(10f);
        using (Fonts.PushSmall())
        {
            ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, y));
            ImGui.PushTextWrapPos(width - pad);
            ImGui.TextColored(Theme.TextFaint, hint);
            ImGui.PopTextWrapPos();
        }
    }

    private static int Sum(int[] numbers, int line) => Lines[line].Sum(i => numbers[i]);

    /// <summary>
    /// La place de la flèche d'une ligne, sa taille et sa direction. Rangées :
    /// à gauche, vers la droite. Colonnes : en tête, vers le bas. Diagonales :
    /// aux coins de la rangée de tête.
    /// </summary>
    private static (Vector2 At, Vector2 Size, Vector2 Direction) ArrowSlot(int line, Vector2 gridMin, float cell, float arrow,
                                                                          float gap)
    {
        var column = arrow + gap;
        return line switch
        {
            < 3 => (gridMin + new Vector2(0f, column + line * (cell + gap)), new Vector2(arrow, cell), new Vector2(1f, 0f)),
            < 6 => (gridMin + new Vector2(column + (line - 3) * (cell + gap), 0f), new Vector2(cell, arrow), new Vector2(0f, 1f)),
            6   => (gridMin, new Vector2(arrow, arrow), Vector2.Normalize(new Vector2(1f, 1f))),
            _   => (gridMin + new Vector2(column + 3f * (cell + gap), 0f), new Vector2(arrow, arrow),
                    Vector2.Normalize(new Vector2(-1f, 1f))),
        };
    }

    /// <summary>Une flèche dessinée, pour ne dépendre d'aucun glyphe de la police.</summary>
    private static void Arrow(ImDrawListPtr dl, Vector2 center, Vector2 direction, uint color)
    {
        var length = Theme.S(10f);
        var tip    = center + direction * length * 0.5f;
        var tail   = center - direction * length * 0.5f;
        var side   = new Vector2(-direction.Y, direction.X) * Theme.S(3.5f);
        var back   = tip - direction * Theme.S(4.5f);

        dl.AddLine(tail, back, color, Theme.S(1.6f));
        dl.AddTriangleFilled(tip, back + side, back - side, color);
    }
}
