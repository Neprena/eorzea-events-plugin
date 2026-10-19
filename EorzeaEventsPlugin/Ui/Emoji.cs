using Dalamud.Interface;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Émojis en tête de ligne, remplacés par une icône plutôt que supprimés.
///
/// Les polices du jeu n'ont pas d'émojis, et <see cref="Glyphs"/> les retire.
/// Mais dans une description, l'émoji de tête de ligne sert de puce : « 🍿 Au
/// programme », « 📍 Rendez-vous », « 💬 Inscriptions ». Retiré, le texte
/// devenait un bloc compact où plus rien ne signalait les rubriques. Vu en jeu
/// le 19 octobre sur une soirée ciné.
///
/// Les émojis courants deviennent l'icône FontAwesome qui leur ressemble ; les
/// autres, un losange. Toute icône ajoutée ici doit aussi figurer dans
/// <see cref="Icons.All"/>, sinon elle s'affichera en carré vide.
/// </summary>
internal static class Emoji
{
    /// <summary>Repli pour un émoji sans équivalent : un losange, comme une puce.</summary>
    public const FontAwesomeIcon Fallback = FontAwesomeIcon.Gem;

    private static readonly Dictionary<int, FontAwesomeIcon> Map = Build();

    /// <summary>Icônes de la table, pour les plages de glyphes de la police.</summary>
    public static IEnumerable<FontAwesomeIcon> AllIcons => Map.Values.Distinct().Append(Fallback);

    private static Dictionary<int, FontAwesomeIcon> Build()
    {
        var map = new Dictionary<int, FontAwesomeIcon>();

        void Add(FontAwesomeIcon icon, params int[] codepoints)
        {
            foreach (var codepoint in codepoints) map[codepoint] = icon;
        }

        void Range(FontAwesomeIcon icon, int first, int last)
        {
            for (var codepoint = first; codepoint <= last; codepoint++) map[codepoint] = icon;
        }

        // Lieu, temps, contact : ce qu'une annonce dit le plus souvent.
        Add(FontAwesomeIcon.MapMarkerAlt,   0x1F4CD, 0x1F5FA);              // 📍 🗺
        Add(FontAwesomeIcon.Thumbtack,      0x1F4CC);                       // 📌
        Add(FontAwesomeIcon.CalendarAlt,    0x1F4C5, 0x1F4C6, 0x1F5D3);     // 📅 📆 🗓
        Add(FontAwesomeIcon.Clock,          0x23F0, 0x231A, 0x23F1, 0x23F2, 0x231B, 0x23F3); // ⏰ ⌚ ⏱ ⏲ ⌛ ⏳
        Range(FontAwesomeIcon.Clock,        0x1F550, 0x1F567);              // 🕐 … 🕧
        Add(FontAwesomeIcon.Comments,       0x1F4AC, 0x1F5E8, 0x1F5EF);     // 💬 🗨 🗯
        Add(FontAwesomeIcon.Envelope,       0x2709, 0x1F4E9, 0x1F4E7, 0x1F48C); // ✉ 📩 📧 💌
        Add(FontAwesomeIcon.Link,           0x1F517);                       // 🔗
        Add(FontAwesomeIcon.Bullhorn,       0x1F4E2, 0x1F4E3);              // 📢 📣
        Add(FontAwesomeIcon.Home,           0x1F3E0, 0x1F3E1, 0x1F3F0, 0x1F3EF); // 🏠 🏡 🏰 🏯

        // Ambiance d'une soirée.
        Add(FontAwesomeIcon.Fire,           0x1F525);                       // 🔥
        Add(FontAwesomeIcon.Music,          0x1F3B5, 0x1F3B6, 0x1F3BC, 0x1F3B9, 0x1F3B8, 0x1F3BB, 0x1F3BA); // 🎵 🎶 🎼 🎹 🎸 🎻 🎺
        Add(FontAwesomeIcon.Headphones,     0x1F3A7);                       // 🎧
        Add(FontAwesomeIcon.Microphone,     0x1F3A4, 0x1F399);              // 🎤 🎙
        Add(FontAwesomeIcon.Film,           0x1F3AC, 0x1F4FD, 0x1F39E);     // 🎬 📽 🎞
        Add(FontAwesomeIcon.Video,          0x1F3A5);                       // 🎥
        Add(FontAwesomeIcon.TheaterMasks,   0x1F3AD);                       // 🎭
        Add(FontAwesomeIcon.Gamepad,        0x1F3AE);                       // 🎮
        Add(FontAwesomeIcon.Dice,           0x1F3B2);                       // 🎲
        Add(FontAwesomeIcon.Gift,           0x1F381);                       // 🎁
        Add(FontAwesomeIcon.Star,           0x2B50, 0x1F31F, 0x2728, 0x1F389, 0x1F38A, 0x1F4AB); // ⭐ 🌟 ✨ 🎉 🎊 💫
        Add(FontAwesomeIcon.Crown,          0x1F451);                       // 👑
        Add(FontAwesomeIcon.Gem,            0x1F48E, 0x1F4B0, 0x1F4B5);     // 💎 💰 💵
        Add(FontAwesomeIcon.Couch,          0x1F6CB);                       // 🛋
        Add(FontAwesomeIcon.Rainbow,        0x1F308);                       // 🌈
        Add(FontAwesomeIcon.Moon,           0x1F319, 0x1F31B, 0x1F31C, 0x1F315, 0x1F316, 0x1F312, 0x1F311); // 🌙 🌛 🌜 🌕 🌖 🌒 🌑
        Add(FontAwesomeIcon.Sun,            0x2600, 0x1F31E, 0x1F305, 0x1F304); // ☀ 🌞 🌅 🌄
        Add(FontAwesomeIcon.Leaf,           0x1F338, 0x1F339, 0x1F33A, 0x1F337, 0x1F33B, 0x1F33C, 0x1F33F, 0x1F340, 0x1F341, 0x1F342); // fleurs et feuilles
        Add(FontAwesomeIcon.Heart,          0x2764, 0x2763);                // ❤ ❣
        Range(FontAwesomeIcon.Heart,        0x1F493, 0x1F49F);              // 💓 … 💟
        Add(FontAwesomeIcon.Heart,          0x1F5A4, 0x1F90D, 0x1F90E, 0x1F9E1); // 🖤 🤍 🤎 🧡

        // Boire et manger.
        Add(FontAwesomeIcon.GlassMartiniAlt, 0x1F379, 0x1F378);             // 🍹 🍸
        Add(FontAwesomeIcon.WineGlassAlt,   0x1F377);                       // 🍷
        Add(FontAwesomeIcon.GlassCheers,    0x1F942);                       // 🥂
        Add(FontAwesomeIcon.Beer,           0x1F37A, 0x1F37B);              // 🍺 🍻
        Add(FontAwesomeIcon.Coffee,         0x2615, 0x1F375);               // ☕ 🍵
        Add(FontAwesomeIcon.Utensils,       0x1F37D, 0x1F374, 0x1F35D, 0x1F355, 0x1F354); // 🍽 🍴 🍝 🍕 🍔
        Add(FontAwesomeIcon.BirthdayCake,   0x1F382, 0x1F370, 0x1F36A, 0x1F37F); // 🎂 🍰 🍪 🍿

        // Jeu de rôle et aventure.
        Add(FontAwesomeIcon.BookOpen,       0x1F4D6, 0x1F4DA, 0x1F4D5, 0x1F4D7, 0x1F4D8, 0x1F4D9, 0x1F4DC); // 📖 📚 📕 … 📜
        Add(FontAwesomeIcon.PenNib,         0x1F58B, 0x1F58A, 0x270D, 0x1F4DD); // 🖋 🖊 ✍ 📝
        Add(FontAwesomeIcon.ShieldAlt,      0x1F6E1);                       // 🛡
        Add(FontAwesomeIcon.Skull,          0x1F480, 0x2620);               // 💀 ☠
        Add(FontAwesomeIcon.Magic,          0x1FA84, 0x1F52E);              // 🪄 🔮
        Add(FontAwesomeIcon.Key,            0x1F511, 0x1F5DD);              // 🔑 🗝
        Add(FontAwesomeIcon.Paw,            0x1F43E);                       // 🐾
        Add(FontAwesomeIcon.Users,          0x1F465, 0x1F46A);              // 👥 👪
        Add(FontAwesomeIcon.Camera,         0x1F4F7, 0x1F4F8);              // 📷 📸

        // Signaux.
        Add(FontAwesomeIcon.ExclamationTriangle, 0x26A0, 0x1F51E, 0x2757, 0x203C); // ⚠ 🔞 ❗ ‼
        Add(FontAwesomeIcon.Check,          0x2705, 0x2714, 0x2611);        // ✅ ✔ ☑
        Add(FontAwesomeIcon.Times,          0x274C, 0x274E, 0x2716);        // ❌ ❎ ✖
        Add(FontAwesomeIcon.HandPointRight, 0x1F449);                       // 👉
        Add(FontAwesomeIcon.InfoCircle,     0x2139, 0x1F4A1);               // ℹ 💡

        return map;
    }

    /// <summary>
    /// Sépare l'émoji de tête d'une ligne du reste du texte.
    /// </summary>
    /// <param name="line">Ligne déjà débarrassée de son retrait.</param>
    /// <param name="icon">Icône de l'émoji, ou le losange de repli s'il n'a pas d'équivalent.</param>
    /// <param name="rest">La ligne sans son émoji ni les espaces qui le suivaient.</param>
    /// <returns>Faux si la ligne ne commence pas par un émoji.</returns>
    public static bool TryLeading(string line, out FontAwesomeIcon icon, out string rest)
    {
        icon = Fallback;
        rest = line;

        var index = 0;
        int? first = null;

        // Un émoji peut en enchaîner d'autres : sélecteurs de variante, jointure
        // (👩‍💻), teintes de peau, ou plusieurs pictogrammes collés (« 🍹🍸 »).
        // Tous partent ; seul le premier donne l'icône.
        while (index < line.Length)
        {
            var codepoint = char.ConvertToUtf32(line, index);
            var length    = char.IsSurrogatePair(line, index) ? 2 : 1;

            if (IsJoiner(codepoint) || (first != null && IsSkinTone(codepoint)))
            {
                index += length;
                continue;
            }

            if (!IsPictograph(codepoint)) break;

            first ??= codepoint;
            index += length;
        }

        if (first is not { } lead) return false;

        icon = Map.TryGetValue(lead, out var mapped) ? mapped : Fallback;
        rest = line[index..].TrimStart();
        return true;
    }

    private static bool IsJoiner(int codepoint) =>
        codepoint is 0x200D or 0xFE0F or 0xFE0E or 0x20E3;

    private static bool IsSkinTone(int codepoint) => codepoint is >= 0x1F3FB and <= 0x1F3FF;

    /// <summary>
    /// Pictogrammes que les polices embarquées n'ont pas, mêmes plages que
    /// celles que <see cref="Glyphs"/> retire. L'alphabet mathématique en est
    /// exclu : Glyphs le ramène à des lettres.
    /// </summary>
    private static bool IsPictograph(int codepoint) =>
        codepoint is >= 0x1F000 and <= 0x1FAFF
                  or >= 0x2600 and <= 0x27BF
                  or >= 0x2B00 and <= 0x2BFF
                  or 0x231A or 0x231B or 0x23F0 or 0x23F1 or 0x23F2 or 0x23F3
                  or 0x2139 or 0x203C or 0x2049;
}
