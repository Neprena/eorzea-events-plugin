using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;
using System.Text;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Rendu Markdown.
///
/// Les descriptions sont rédigées sur le site avec un éditeur Markdown, mais le
/// plugin les affichait telles quelles, astérisques et dièses compris, ou bien
/// les aplatissait en retirant toute mise en forme.
///
/// ImGui n'a pas de texte enrichi : le rendu procède segment par segment, avec
/// un retour à la ligne calculé à la main, la police variant d'un segment à
/// l'autre. L'analyse est mémorisée, faute de quoi elle serait refaite à chaque
/// frame pour chaque description.
///
/// Le sous-ensemble reconnu est celui que les rédacteurs utilisent réellement :
/// titres, gras, italique, code, liens, listes, citations et filets.
/// </summary>
internal static class MarkdownView
{
    private static readonly Dictionary<string, List<Block>> Cache = [];
    private const int CacheLimit = 128;

    public static void Draw(string? markdown, Vector4? baseColor = null)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return;

        var afterHeading = false;
        foreach (var block in Parse(markdown))
        {
            DrawBlock(block, baseColor ?? Theme.Text, afterHeading);
            afterHeading = block.Kind == BlockKind.Heading;
        }
    }

    // ─── Rendu ────────────────────────────────────────────────────────────────

    /// <param name="afterHeading">
    /// Le bloc précédent était un titre. La ligne vide qui suit d'ordinaire un
    /// titre ne doit pas creuser un blanc de paragraphe : le titre en a déjà un
    /// au-dessus, et un second l'éloignait du texte qu'il annonce.
    /// </param>
    private static void DrawBlock(Block block, Vector4 baseColor, bool afterHeading = false)
    {
        switch (block.Kind)
        {
            case BlockKind.Rule:
                Layout.Divider(Theme.GapS);
                return;

            case BlockKind.Heading:
            {
                // De l'air avant un titre plus qu'entre deux paragraphes : c'est
                // ce qui le rattache au texte qui suit plutôt qu'à celui d'avant.
                Layout.Spacer(Theme.GapL);

                // L'or des titres et non l'accent : en bleu clair, un intertitre
                // se confondait avec un lien.
                var major = block.Level <= 2;
                var tint  = major ? Theme.TitleGold : Theme.Text;
                Func<IDisposable> font = major ? Fonts.PushH2 : Fonts.PushBodyStrong;

                using (font())
                {
                    if (block.Lead is { } headIcon)
                    {
                        ImGui.TextColored(tint, headIcon.S());
                        ImGui.SameLine(0f, Theme.S(Theme.GapS));
                    }
                    DrawSpans(block.Spans, tint, font: font);
                }

                // Un filet sous les titres de premier rang : la taille et la
                // couleur seules ne les détachaient pas assez du corps, et un
                // titre qui ne se voit pas ne sert à rien dans un texte long.
                // Un filet nu, collé au titre : Layout.Divider l'entourait de
                // deux marges, qui laissaient un vide entre le titre et son texte.
                if (major) ImGui.Separator();
                return;
            }

            case BlockKind.Bullet:
                // Puce des notes de version : l'étiquette de la ligne remplace
                // le point par son icône, et se teinte de la même couleur.
                if (block.Tag != NoteTag.None)
                {
                    var (icon, tint) = TagStyle(block.Tag);
                    DrawMarker(icon.S(), block.Indent, tint);
                    DrawSpans(block.Spans, baseColor, BulletIndent(block.Indent), tint);
                    return;
                }

                if (block.Lead is { } bulletIcon) DrawMarker(bulletIcon.S(), block.Indent, Theme.TitleGold);
                else                              DrawMarker("•", block.Indent);
                DrawSpans(block.Spans, baseColor, BulletIndent(block.Indent));
                return;

            case BlockKind.Numbered:
                DrawMarker($"{block.Number}.", block.Indent);
                DrawSpans(block.Spans, baseColor, BulletIndent(block.Indent));
                return;

            case BlockKind.Quote:
                DrawQuote(block);
                return;

            default:
                // L'air se met avant un paragraphe, et seulement quand une ligne
                // vide l'annonce. Posé après chaque ligne, il transformait un
                // texte écrit au fil des retours à la ligne en une suite de
                // paragraphes espacés.
                //
                // Les deux tiers d'une ligne, et non plus 3 px : vu en jeu le
                // 19 octobre, les lignes vides d'une description disparaissaient
                // et tous les paragraphes se suivaient sans respiration.
                if (block.BlankBefore && !afterHeading)
                    ImGui.Dummy(new Vector2(0f, MathF.Round(ImGui.GetTextLineHeight() * 0.65f)));

                // L'émoji de tête devenu icône, en or, à la place d'une puce.
                if (block.Lead is { } leadIcon)
                {
                    DrawMarker(leadIcon.S(), block.Indent, Theme.TitleGold);
                    DrawSpans(block.Spans, baseColor, BulletIndent(block.Indent));
                    return;
                }

                DrawSpans(block.Spans, baseColor);
                return;
        }
    }

    private static float BulletIndent(int level) => Theme.S(16f + level * 14f);

    private static void DrawMarker(string marker, int level, Vector4? color = null)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Theme.S(4f + level * 14f));
        ImGui.TextColored(color ?? Theme.TextFaint, marker);
        ImGui.SameLine(0f, Theme.S(Theme.GapS));
    }

    /// <summary>Icône et teinte d'une étiquette de note de version.</summary>
    private static (FontAwesomeIcon Icon, Vector4 Color) TagStyle(NoteTag tag) => tag switch
    {
        NoteTag.New      => (Icons.ChangeNew,      Theme.Online),
        NoteTag.Improved => (Icons.ChangeImproved, Theme.Link),
        NoteTag.Fixed    => (Icons.ChangeFixed,    Theme.Idle),
        NoteTag.Security => (Icons.ChangeSecurity, Theme.Gold),
        _                => (Icons.ChangeThanks,   Theme.Danger),
    };

    private static void DrawQuote(Block block)
    {
        var start = ImGui.GetCursorScreenPos();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Theme.S(Theme.GapM));
        DrawSpans(block.Spans, Theme.TextMuted, Theme.S(Theme.GapM));

        var end = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddRectFilled(
            start,
            new Vector2(start.X + Theme.S(2f), end.Y - Theme.S(Theme.GapXs)),
            ImGui.GetColorU32(Theme.Alpha(Theme.Accent, 0.6f)),
            Theme.S(1f));

        Layout.Spacer(Theme.GapXs);
    }

    /// <summary>
    /// Écrit une suite de segments en repliant sur la largeur utile. Le retour à
    /// la ligne est calculé mot à mot : la police variant d'un segment à
    /// l'autre, le repli automatique d'ImGui ne peut pas s'appliquer.
    /// </summary>
    /// <param name="leadColor">
    /// Teinte du premier segment, quand il porte l'étiquette d'une note de
    /// version (« Nouveauté : », « Correction : »…). Le reste de la ligne garde
    /// la couleur du corps.
    /// </param>
    /// <param name="font">
    /// Police imposée à tous les segments, pour un titre. Sans elle, chaque
    /// segment pousse la police du corps, qui écrasait celle du titre : les
    /// intertitres sortaient à la taille du texte.
    /// </param>
    private static void DrawSpans(List<Span> spans, Vector4 color, float indent = 0f,
                                  Vector4? leadColor = null, Func<IDisposable>? font = null)
    {
        var left     = ImGui.GetCursorPosX();
        var maxWidth = ImGui.GetContentRegionAvail().X - Card.RightInset - indent;

        // La largeur consommée est suivie à la main. Interroger le curseur ne
        // marcherait pas : après un texte, son abscisse est déjà revenue en
        // début de ligne suivante, et la comparaison serait toujours vraie.
        var used = 0f;

        for (var index = 0; index < spans.Count; index++)
        {
            var span = spans[index];

            using var spanFont = font != null                       ? font()
                               : span.Style.HasFlag(SpanStyle.Bold) ? Fonts.PushBodyStrong()
                               :                                      Fonts.PushBody();

            var tint = span.Url != null                        ? Theme.Link
                     : index == 0 && leadColor is { } lead     ? lead
                     : span.Style.HasFlag(SpanStyle.Code)      ? Theme.Gold
                     : span.Style.HasFlag(SpanStyle.Italic)    ? Theme.TextMuted
                     : color;

            var spacing = ImGui.CalcTextSize(" ").X;

            foreach (var word in span.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var width = ImGui.CalcTextSize(word).X;

                if (used > 0f)
                {
                    if (used + spacing + width <= maxWidth)
                    {
                        ImGui.SameLine(0f, spacing);
                        used += spacing;
                    }
                    else
                    {
                        used = 0f; // le mot ouvre une nouvelle ligne
                    }
                }

                if (used == 0f && indent > 0f)
                    ImGui.SetCursorPosX(left + indent);

                ImGui.TextColored(tint, word);
                used += width;

                if (span.Url == null || !ImGui.IsItemHovered()) continue;

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                Feedback.Tooltip(span.Url);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) Open(span.Url);
            }
        }
    }

    private static void Open(string url)
    {
        // Les textes rédigés sur le site y lient leurs pages en chemin relatif
        // (« /aide ») : hors du site, il faut leur rendre l'adresse de celui-ci.
        if (url.StartsWith('/')) url = Plugin.Config.BaseUrl.TrimEnd('/') + url;

        try { Dalamud.Utility.Util.OpenLink(url); }
        catch { /* le navigateur peut être indisponible */ }
    }

    // ─── Analyse ──────────────────────────────────────────────────────────────

    private static List<Block> Parse(string markdown)
    {
        if (Cache.TryGetValue(markdown, out var cached)) return cached;

        // Le texte n'est plus nettoyé d'un bloc : chaque ligne l'est à son tour,
        // après qu'on a relevé l'émoji qui l'ouvre, que le nettoyage effacerait.
        var blocks = ParseBlocks(markdown.Replace("\r", string.Empty));

        if (Cache.Count >= CacheLimit) Cache.Clear();
        Cache[markdown] = blocks;
        return blocks;
    }

    private static List<Block> ParseBlocks(string text)
    {
        var blocks = new List<Block>();

        // Retenue d'une ligne vide à la suivante : c'est elle qui fait un
        // paragraphe, là où un simple retour à la ligne n'en fait pas.
        var blank = false;

        foreach (var raw in text.Split('\n'))
        {
            var indent = CountIndent(raw);
            var source = raw.Trim();

            // Un émoji en tête de ligne, ou juste après une puce, devient une
            // icône : c'est lui qui marquait la rubrique.
            FontAwesomeIcon? lead = null;
            var bulleted = source.Length > 1 && source[0] is '-' or '*' or '+' && source[1] == ' ';
            var body     = bulleted ? source[2..].TrimStart() : source;
            if (Emoji.TryLeading(body, out var icon, out var rest))
            {
                lead   = icon;
                source = bulleted ? $"{source[0]} {rest}" : rest;
            }

            var trimmed = Glyphs.Safe(source);
            if (trimmed.Length == 0)
            {
                // Une ligne faite d'un seul émoji reste une ligne vide.
                blank = true;
                continue;
            }

            // Filet horizontal.
            if (trimmed is "---" or "***" or "___")
            {
                blocks.Add(new Block(BlockKind.Rule, [], 0, indent, 0));
                continue;
            }

            // Titre.
            if (trimmed.StartsWith('#'))
            {
                var level = 0;
                while (level < trimmed.Length && trimmed[level] == '#') level++;
                if (level <= 6 && level < trimmed.Length && trimmed[level] == ' ')
                {
                    // L'émoji d'un titre suit les dièses : relevé ici, sur la
                    // ligne d'origine, avant que le nettoyage ne l'efface.
                    var heading = source.TrimStart('#').TrimStart();
                    FontAwesomeIcon? headLead = null;
                    if (Emoji.TryLeading(heading, out var headIcon, out var headRest))
                    {
                        headLead = headIcon;
                        heading  = headRest;
                    }

                    blocks.Add(new Block(BlockKind.Heading, ParseSpans(Glyphs.Safe(heading)),
                                         level, indent, 0, Lead: headLead));
                    continue;
                }
            }

            // Citation.
            if (trimmed.StartsWith("> "))
            {
                blocks.Add(new Block(BlockKind.Quote, ParseSpans(trimmed[2..]), 0, indent, 0));
                continue;
            }

            // Liste à puces.
            if (trimmed.Length > 1 && trimmed[0] is '-' or '*' or '+' && trimmed[1] == ' ')
            {
                var item = trimmed[2..];
                blocks.Add(new Block(BlockKind.Bullet, ParseSpans(item), 0, indent, 0,
                                     Tag: DetectTag(item), Lead: lead));
                continue;
            }

            // Liste numérotée.
            var dot = trimmed.IndexOf('.');
            if (dot is > 0 and < 4 && int.TryParse(trimmed[..dot], out var number)
                && dot + 1 < trimmed.Length && trimmed[dot + 1] == ' ')
            {
                blocks.Add(new Block(BlockKind.Numbered, ParseSpans(trimmed[(dot + 2)..]),
                                     0, indent, number));
                continue;
            }

            blocks.Add(new Block(BlockKind.Paragraph, ParseSpans(trimmed), 0, indent, 0, blank, Lead: lead));
            blank = false;
        }

        return blocks;
    }

    private static int CountIndent(string line)
    {
        var spaces = 0;
        while (spaces < line.Length && line[spaces] == ' ') spaces++;
        return Math.Min(spaces / 2, 3);
    }

    /// <summary>
    /// Étiquette portée par une puce de note de version, ou
    /// <see cref="NoteTag.None"/> pour toute autre ligne.
    ///
    /// Le vocabulaire est fermé et la recherche bornée aux premiers caractères :
    /// une phrase ordinaire qui contiendrait « : » plus loin ne peut pas être
    /// prise pour une étiquette.
    /// </summary>
    private static NoteTag DetectTag(string item)
    {
        var text  = item.TrimStart('*', ' ');
        var colon = text.IndexOf(':');
        if (colon is <= 0 or > 16) return NoteTag.None;

        // L'espace avant le deux-points est une convention typographique
        // française, et peut être fine ou insécable selon qui a saisi la ligne.
        var label = text[..colon].TrimEnd('*', ' ', '\u00A0', '\u202F');

        return label.ToLowerInvariant() switch
        {
            "nouveauté"   or "new"         => NoteTag.New,
            "amélioration" or "improvement" => NoteTag.Improved,
            "correction"  or "fix"         => NoteTag.Fixed,
            "sécurité"    or "security"    => NoteTag.Security,
            "merci"       or "thanks"      => NoteTag.Thanks,
            _                              => NoteTag.None,
        };
    }

    /// <summary>Découpe une ligne en segments stylés.</summary>
    private static List<Span> ParseSpans(string line)
    {
        var spans  = new List<Span>();
        var buffer = new StringBuilder();
        var style  = SpanStyle.None;

        void Flush()
        {
            if (buffer.Length == 0) return;
            spans.Add(new Span(buffer.ToString(), style, null));
            buffer.Clear();
        }

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            // Lien : [libellé](adresse)
            if (c == '[')
            {
                var close = line.IndexOf(']', i);
                var open  = close >= 0 && close + 1 < line.Length && line[close + 1] == '('
                          ? close + 1
                          : -1;
                var end   = open > 0 ? line.IndexOf(')', open) : -1;

                if (end > 0)
                {
                    Flush();
                    spans.Add(new Span(line[(i + 1)..close], style, line[(open + 1)..end]));
                    i = end;
                    continue;
                }
            }

            if (c == '`')
            {
                Flush();
                style ^= SpanStyle.Code;
                continue;
            }

            if (c is '*' or '_')
            {
                var doubled = i + 1 < line.Length && line[i + 1] == c;
                Flush();
                style ^= doubled ? SpanStyle.Bold : SpanStyle.Italic;
                if (doubled) i++;
                continue;
            }

            buffer.Append(c);
        }

        Flush();
        return spans;
    }

    // ─── Modèle ───────────────────────────────────────────────────────────────

    private enum BlockKind { Paragraph, Heading, Bullet, Numbered, Quote, Rule }

    /// <summary>
    /// Étiquette d'une puce de note de version. Voir <see cref="ReleaseNotes"/>,
    /// dont chaque ligne commence par l'une d'elles.
    /// </summary>
    private enum NoteTag { None, New, Improved, Fixed, Security, Thanks }

    [Flags]
    private enum SpanStyle { None = 0, Bold = 1, Italic = 2, Code = 4 }

    /// <param name="BlankBefore">
    /// Une ligne vide précédait ce bloc dans le texte source.
    ///
    /// C'est ce qui sépare un retour à la ligne d'un changement de paragraphe.
    /// Sans cette distinction, chaque appui sur Entrée valait un paragraphe, et
    /// un texte écrit au fil des lignes s'affichait aéré comme autant de
    /// paragraphes.
    /// </param>
    /// <param name="Lead">Icône de l'émoji qui ouvrait la ligne, posée en guise de puce.</param>
    private sealed record Block(BlockKind Kind, List<Span> Spans, int Level, int Indent, int Number,
                                bool BlankBefore = false, NoteTag Tag = NoteTag.None,
                                FontAwesomeIcon? Lead = null);

    private readonly record struct Span(string Text, SpanStyle Style, string? Url);
}
