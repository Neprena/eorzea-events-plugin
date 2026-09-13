using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Zone de saisie multiligne dont les lignes se replient sur la largeur du champ.
///
/// ImGui ne replie jamais le texte d'une zone de saisie : un paragraphe écrit
/// d'un trait file vers la droite et sort du cadre, quelle que soit la largeur
/// de la fenêtre. Les descriptions RP s'écrivent en paragraphes, le champ
/// devenait donc illisible dès la première ligne pleine.
///
/// Le repli est calculé ici, mot à mot, puis poussé dans le tampon d'ImGui
/// depuis un rappel <see cref="ImGuiInputTextFlags.CallbackAlways"/> : tant que
/// le champ a le curseur, ImGui travaille sur sa copie interne et ignore le
/// tampon de l'appelant. Le rappel est le seul endroit d'où l'on peut replier
/// sous les doigts de qui écrit.
///
/// Les sauts ajoutés n'entrent pas dans le texte rendu : leurs positions sont
/// mémorisées et retirées avant que la valeur ne reparte à l'appelant. Une
/// description enregistrée reste donc celle qui a été tapée. Écrits dans le
/// texte, ces replis se verraient sur le site comme en jeu, et resteraient figés
/// à la largeur qu'avait la fenêtre le jour de la rédaction.
/// </summary>
internal static class TextArea
{
    /// <summary>Ce qu'un champ garde d'une image à l'autre.</summary>
    private sealed class Field
    {
        /// <summary>Le texte tel qu'il sera enregistré, sans les replis.</summary>
        public string Source = string.Empty;

        /// <summary>Le texte tel qu'ImGui l'affiche, replis compris.</summary>
        public string Display = string.Empty;

        /// <summary>Position, dans <see cref="Display"/>, des sauts ajoutés.</summary>
        public List<int> Softs = [];

        /// <summary>Le tampon laissé à la dernière image, pour repérer une frappe.</summary>
        public byte[] Previous = [];
        public int    PreviousLength;

        public float Width    = -1f;
        public int   MaxChars;

        /// <summary>Le champ avait le curseur à la dernière image.</summary>
        public bool Active;

        /// <summary>La largeur a changé pendant la saisie : le rappel doit replier.</summary>
        public bool WrapDirty;

        /// <summary>Dernière image où le champ a été dessiné.</summary>
        public int LastFrame = -2;

        public ImGui.ImGuiInputTextCallbackPtrDelegate Callback = null!;
    }

    private static readonly Dictionary<string, Field> Fields = [];

    /// <summary>
    /// Dessine la zone de saisie. <paramref name="maxChars"/> compte des
    /// caractères, comme le compteur affiché et comme la limite du site : le
    /// tampon donné à ImGui est dimensionné en octets, bien plus large, pour
    /// qu'un texte accentué ne soit pas tronqué avant sa limite.
    /// </summary>
    public static bool Draw(string id, ref string text, int maxChars, Vector2 size)
    {
        if (!Fields.TryGetValue(id, out var field))
        {
            field = new Field();
            field.Callback = data => Callback(field, data);
            Fields[id] = field;
        }

        // Un champ qui n'était pas dessiné à l'image précédente vient d'une
        // fenêtre rouverte : son curseur est perdu depuis longtemps, et croire
        // l'inverse ferait afficher le texte de la fiche précédente.
        var frame = ImGui.GetFrameCount();
        if (frame - field.LastFrame > 1) field.Active = false;
        field.LastFrame = frame;

        field.MaxChars = maxChars;

        // Largeur utile du cadre : ImGui pose une marge de chaque côté, et
        // réserve la barre de défilement dès que le texte dépasse en hauteur,
        // ce qui est le cas de tout texte assez long pour avoir besoin du repli.
        var style  = ImGui.GetStyle();
        var box    = size.X > 0f ? size.X : ImGui.GetContentRegionAvail().X + size.X;
        var width  = MathF.Max(Theme.S(40f),
                               box - style.FramePadding.X * 2f - style.ScrollbarSize - Theme.S(4f));

        var moved = text != field.Source;
        var resized = MathF.Abs(width - field.Width) > 0.5f;
        field.Width = width;

        if (!field.Active && (moved || resized))
        {
            field.Source = text;
            Rewrap(field);
        }
        else if (resized)
        {
            // Le tampon de l'appelant n'est pas lu pendant la saisie : c'est au
            // rappel de replier, à partir de ce qu'ImGui a réellement en main.
            field.WrapDirty = true;
        }

        var before   = field.Source;
        var capacity = maxChars * 3 + 1024;

        ImGui.InputTextMultiline(id, ref field.Display, capacity, size,
                                 ImGuiInputTextFlags.CallbackAlways, field.Callback);

        field.Active = ImGui.IsItemActive();

        // Le repli réécrit le tampon sous ImGui, qui signale alors une
        // modification même quand rien n'a été tapé : c'est le texte sans repli
        // qui dit s'il y a eu changement.
        if (field.Source == before) return false;

        text = field.Source;
        return true;
    }

    /// <summary>Oublie l'état d'un champ. Appelé quand son contenu change de sujet.</summary>
    public static void Forget(string id) => Fields.Remove(id);

    // ─── Rappel de saisie ─────────────────────────────────────────────────────

    private static int Callback(Field field, ImGuiInputTextCallbackDataPtr data)
    {
        var buffer = data.BufTextSpan;

        // Rien n'a bougé : ni une frappe, ni la largeur. Le cas de loin le plus
        // fréquent, soixante fois par seconde, et il ne coûte qu'une comparaison.
        if (!field.WrapDirty && buffer.SequenceEqual(field.Previous.AsSpan(0, field.PreviousLength)))
            return 0;

        field.WrapDirty = false;

        var display = Encoding.UTF8.GetString(buffer);

        // Les replis d'avant la frappe se retrouvent dans le texte modifié : ceux
        // qui précèdent la retouche n'ont pas bougé, ceux qui la suivent ont
        // glissé de ce qu'elle a ajouté ou retiré, ceux qui étaient dedans ont
        // disparu avec elle.
        var softs  = AdjustSofts(field.Display, display, field.Softs);
        var source = Clamp(Strip(display, softs), field.MaxChars);

        var (wrapped, newSofts) = Wrap(source, field.Width);

        if (wrapped != display)
        {
            // Le curseur est repéré dans le texte sans repli, seul repère qui
            // survive au recalcul, puis reporté dans le texte replié.
            var cursorByte = Math.Clamp(data.CursorPos, 0, buffer.Length);
            var cursorChar = Encoding.UTF8.GetCharCount(buffer[..cursorByte]);
            var cursor     = ToDisplay(ToSource(cursorChar, softs), newSofts);

            data.DeleteChars(0, data.BufTextLen);
            data.InsertChars(0, wrapped);
            data.CursorPos = Encoding.UTF8.GetByteCount(
                wrapped.AsSpan(0, Math.Clamp(cursor, 0, wrapped.Length)));
            data.ClearSelection();
        }

        field.Source  = source;
        Remember(field, wrapped, newSofts);
        return 0;
    }

    private static void Rewrap(Field field)
    {
        var (display, softs) = Wrap(field.Source, field.Width);
        Remember(field, display, softs);
    }

    private static void Remember(Field field, string display, List<int> softs)
    {
        field.Display        = display;
        field.Softs          = softs;
        field.Previous       = Encoding.UTF8.GetBytes(display);
        field.PreviousLength = field.Previous.Length;
    }

    // ─── Repli ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replie le texte sur <paramref name="width"/> et rend les positions des
    /// sauts ajoutés. La mesure se fait mot à mot avec la police du champ :
    /// ImGui additionne les largeurs de glyphe sans crénage, la somme des mots
    /// est donc exactement la largeur de la ligne.
    /// </summary>
    private static (string Display, List<int> Softs) Wrap(string source, float width)
    {
        var softs = new List<int>();
        if (width <= 1f || source.Length == 0) return (source, softs);

        var display = new StringBuilder(source.Length + 64);
        var line    = 0f;
        var index   = 0;

        while (index < source.Length)
        {
            if (source[index] == '\n')
            {
                display.Append('\n');
                line = 0f;
                index++;
                continue;
            }

            // Un bloc : les espaces qui précèdent le mot, puis le mot. La coupure
            // se place entre les deux et jamais avant les espaces : en fin de
            // ligne elles ne se voient pas, en tête de la suivante elles
            // décaleraient le mot d'un cran.
            var blockStart = index;
            while (index < source.Length && IsSpace(source[index])) index++;
            var wordStart = index;
            while (index < source.Length && !IsSpace(source[index]) && source[index] != '\n') index++;

            var lead = source.AsSpan(blockStart, wordStart - blockStart);
            var word = source.AsSpan(wordStart, index - wordStart);

            var leadWidth = Measure(lead);
            var wordWidth = Measure(word);

            display.Append(lead);

            if (line > 0f && line + leadWidth + wordWidth > width)
            {
                softs.Add(display.Length);
                display.Append('\n');
                line = 0f;
            }
            else
            {
                line += leadWidth;
            }

            // Un mot plus large que le champ, une adresse ou un nom sans espace,
            // ne tient sur aucune ligne : il se coupe caractère par caractère,
            // sans quoi il repartirait seul vers la droite.
            if (wordWidth > width)
            {
                for (var i = 0; i < word.Length; i++)
                {
                    var span  = char.IsHighSurrogate(word[i]) && i + 1 < word.Length ? 2 : 1;
                    var glyph = word.Slice(i, span);
                    var size  = Measure(glyph);

                    if (line > 0f && line + size > width)
                    {
                        softs.Add(display.Length);
                        display.Append('\n');
                        line = 0f;
                    }

                    display.Append(glyph);
                    line += size;
                    i    += span - 1;
                }
            }
            else
            {
                display.Append(word);
                line += wordWidth;
            }
        }

        return (display.ToString(), softs);
    }

    private static bool IsSpace(char c) => c == ' ' || c == '\t';

    private static float Measure(ReadOnlySpan<char> text)
        => text.IsEmpty ? 0f : ImGui.CalcTextSize(text).X;

    // ─── Correspondance entre le texte affiché et le texte gardé ──────────────

    private static List<int> AdjustSofts(string previous, string current, List<int> softs)
    {
        if (softs.Count == 0) return [];

        var min  = Math.Min(previous.Length, current.Length);
        var head = 0;
        while (head < min && previous[head] == current[head]) head++;

        var tail = 0;
        while (tail < min - head
               && previous[previous.Length - 1 - tail] == current[current.Length - 1 - tail])
            tail++;

        var end   = previous.Length - tail;
        var delta = current.Length - previous.Length;

        var moved = new List<int>(softs.Count);
        foreach (var soft in softs)
        {
            if      (soft <  head) moved.Add(soft);
            else if (soft >= end)  moved.Add(soft + delta);
        }

        return moved;
    }

    /// <summary>Retire les sauts ajoutés pour retrouver le texte tapé.</summary>
    private static string Strip(string display, List<int> softs)
    {
        if (softs.Count == 0) return display;

        var source = new StringBuilder(display.Length);
        var next   = 0;

        for (var i = 0; i < display.Length; i++)
        {
            if (next < softs.Count && softs[next] == i)
            {
                next++;

                // Une position qui ne tombe plus sur un saut a dérivé : mieux
                // vaut un repli de trop qu'une lettre mangée dans la phrase.
                if (display[i] == '\n') continue;
            }

            source.Append(display[i]);
        }

        return source.ToString();
    }

    private static string Clamp(string text, int maxChars)
    {
        if (maxChars <= 0 || text.Length <= maxChars) return text;

        var cut = maxChars;
        if (char.IsHighSurrogate(text[cut - 1])) cut--;
        return text[..cut];
    }

    private static int ToSource(int position, List<int> softs)
    {
        var shift = 0;
        foreach (var soft in softs)
        {
            if (soft >= position) break;
            shift++;
        }

        return position - shift;
    }

    private static int ToDisplay(int position, List<int> softs)
    {
        foreach (var soft in softs)
        {
            if (soft > position) break;
            position++;
        }

        return position;
    }
}
