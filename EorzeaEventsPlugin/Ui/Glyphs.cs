using System.Text;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Normalise les textes venus de l'API pour qu'ils restent lisibles avec les
/// polices embarquées.
///
/// Les noms d'établissements et les descriptions sont rédigés par des joueurs,
/// qui emploient volontiers des caractères décoratifs qu'aucune police de texte
/// ne couvre : pleine chasse, alphabet mathématique et émojis. Sans traitement,
/// ils s'affichent en points d'interrogation.
///
/// La normalisation de compatibilité Unicode (NFKC) ramène les deux premières
/// familles à leur équivalent latin, ce qui les rend à la fois affichables et
/// plus lisibles. Les émojis, eux, sont retirés : il n'existe pas de repli
/// raisonnable pour eux dans une police vectorielle monochrome.
/// </summary>
internal static class Glyphs
{
    private static readonly Dictionary<string, string> Cache = [];

    /// <summary>Au-delà, le cache est vidé : il n'évite qu'un recalcul par frame.</summary>
    private const int CacheLimit = 512;

    /// <summary>
    /// Symboles absents d'Inter mais assez courants pour mériter un équivalent
    /// plutôt qu'une suppression.
    /// </summary>
    private static readonly Dictionary<char, string> Substitutes = new()
    {
        // Les joueurs substituent volontiers une lettre par un symbole qui lui
        // ressemble. « ＦＡＣＴ⚙ＲＹ » attend un O, pas une suppression.
        ['⚙'] = "O",
        ['☼'] = "O",
        ['✿'] = "o",
        ['❀'] = "o",
        ['★'] = "*",
        ['☆'] = "*",
        ['✦'] = "◆", // étoile à quatre branches vers losange plein
        ['✧'] = "◇",
        ['➡'] = "→", // flèche épaisse vers flèche simple
        ['✨'] = "",   // étincelles
        ['☠'] = "",   // tête de mort
        ['❤'] = "",   // cœur
        ['　'] = " ",  // espace pleine chasse
    };

    /// <summary>
    /// Rend un texte affichable. Le résultat est mémorisé : la méthode est
    /// appelée à chaque frame, pour chaque élément de liste.
    /// </summary>
    public static string Safe(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (Cache.TryGetValue(text, out var cached)) return cached;

        var result = Convert(text);

        if (Cache.Count >= CacheLimit) Cache.Clear();
        Cache[text] = result;
        return result;
    }

    /// <summary>
    /// Réduit un texte long à un résumé d'une ligne : la syntaxe Markdown est
    /// retirée, les sauts de ligne aplatis, et la coupe se fait sur un mot.
    /// </summary>
    public static string Summarize(string? text, int maxLength)
    {
        var safe = Safe(text);
        if (safe.Length == 0) return string.Empty;

        var builder = new StringBuilder(safe.Length);
        var space   = false;

        foreach (var ch in safe)
        {
            // La syntaxe Markdown n'est pas rendue ici : la retirer évite
            // d'afficher des astérisques et des dièses isolés.
            if (ch is '*' or '#' or '_' or '`' or '>' or '~') continue;

            if (char.IsWhiteSpace(ch))
            {
                space = true;
                continue;
            }

            if (space && builder.Length > 0) builder.Append(' ');
            space = false;
            builder.Append(ch);
        }

        var flat = builder.ToString().Trim();
        if (flat.Length <= maxLength) return flat;

        var cut = flat.LastIndexOf(' ', Math.Min(maxLength, flat.Length - 1));
        if (cut < maxLength / 2) cut = maxLength;

        return string.Concat(flat.AsSpan(0, cut).TrimEnd(), "…");
    }

    private static string Convert(string text)
    {
        var builder = new StringBuilder(text.Length);
        var removed = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            // ── Plan astral ───────────────────────────────────────────────────
            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                var codepoint = char.ConvertToUtf32(ch, text[i + 1]);
                i++;

                // Alphabet mathématique : la décomposition de compatibilité en
                // donne la lettre latine correspondante.
                if (codepoint is >= 0x1D400 and <= 0x1D7FF)
                {
                    Flush(builder, ref removed, ' ');
                    builder.Append(char.ConvertFromUtf32(codepoint).Normalize(NormalizationForm.FormKC));
                    continue;
                }

                // Tout le reste du plan astral est hors de portée des polices
                // embarquées : émojis, drapeaux, pictogrammes.
                removed = true;
                continue;
            }

            // ── Marques invisibles ────────────────────────────────────────────
            if (ch is >= '︀' and <= '️' || ch is '‍' or '﻿')
                continue;

            // ── Pleine chasse ─────────────────────────────────────────────────
            if (ch is >= '！' and <= '～')
            {
                Flush(builder, ref removed, ' ');
                builder.Append((char)(ch - 0xFEE0));
                continue;
            }

            // ── Substitutions explicites ──────────────────────────────────────
            if (Substitutes.TryGetValue(ch, out var replacement))
            {
                if (replacement.Length == 0)
                {
                    removed = true;
                }
                else
                {
                    Flush(builder, ref removed, replacement[0]);
                    builder.Append(replacement);
                }
                continue;
            }

            // ── Symboles non couverts par Inter ───────────────────────────────
            if (ch is >= '☀' and <= '➿' or >= '⬀' and <= '⯿')
            {
                removed = true;
                continue;
            }

            Flush(builder, ref removed, ch);
            builder.Append(ch);
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// Insère une espace si un caractère vient d'être retiré, pour ne pas
    /// coller les mots voisins, et sans créer de double espace.
    /// </summary>
    private static void Flush(StringBuilder builder, ref bool removed, char next)
    {
        if (!removed) return;
        removed = false;

        if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]) && !char.IsWhiteSpace(next))
            builder.Append(' ');
    }
}
