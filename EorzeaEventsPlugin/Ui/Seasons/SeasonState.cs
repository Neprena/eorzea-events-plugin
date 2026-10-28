namespace EorzeaEventsPlugin.Ui.Seasons;

/// <summary>
/// Le thème en vigueur, recalculé au début de chaque image, avant que la
/// moindre fenêtre ne lise <see cref="Theme"/>.
///
/// Les joueurs n'ont qu'un choix : selon la date, ou la Nuit dorée toute
/// l'année. Un thème ne se voit pas hors de sa période ; seule une compilation
/// de débogage peut simuler une autre date, depuis les paramètres de
/// développement.
/// </summary>
internal static class SeasonState
{
    /// <summary>
    /// Le thème du calendrier, recalculé chaque minute et à chaque nouvelle liste
    /// officielle : les périodes de Square Enix commencent et finissent en cours
    /// de journée (8:00 et 14:59 GMT).
    /// </summary>
    private static DateTime _checkedAt;
    private static int      _version = -1;
    private static Season   _dated;

    /// <summary>La palette posée dans <see cref="Theme"/>, pour ne la remplacer qu'au changement.</summary>
    private static Season? _applied;

    public static Season Current { get; private set; }

    /// <summary>Le 1er avril, thèmes automatiques seulement.</summary>
    public static bool AprilFools { get; private set; }

    /// <summary>Le décor de la saison est à peindre.</summary>
    public static bool Decor => Current != Season.None && Plugin.Config.SeasonalDecor;

    /// <summary>Les surprises (chasses, calendrier, fantôme, mogs) sont actives.</summary>
    public static bool EasterEggs => Plugin.Config.SeasonalThemes && Plugin.Config.SeasonalEasterEggs;

    /// <summary>
    /// L'horloge du décor. Figée à zéro quand Dalamud demande des animations
    /// réduites : tout reste en place, rien ne bouge.
    /// </summary>
    public static float Time => Plugin.PluginInterface.UiBuilder.ShouldUseReducedMotion ? 0f : (float)Dalamud.Bindings.ImGui.ImGui.GetTime();

    /// <summary>
    /// L'heure des thèmes : celle de l'ordinateur du joueur. Tout le code des
    /// saisons passe par elle, jamais par <see cref="DateTime.Now"/>, pour que
    /// la date simulée du débogage s'applique partout à la fois.
    /// </summary>
    public static DateTime Now
    {
        get
        {
#if DEBUG
            // La date simulée avance ensuite au rythme réel : le compte à rebours
            // du 31 décembre défile et franchit minuit.
            if (_simulated is { } start) return start + (DateTime.Now - _simulatedSince);
#endif
            return DateTime.Now;
        }
    }

#if DEBUG
    private static DateTime? _simulated;
    private static DateTime  _simulatedSince;

    /// <summary>La date de départ simulée, ou null pour la date réelle.</summary>
    public static DateTime? Simulated => _simulated;

    /// <summary>Le fantôme passe en boucle, sans attendre minuit à Eorzea.</summary>
    public static bool ForceGhost { get; set; }

    public static void Simulate(DateTime? date)
    {
        _simulated      = date;
        _simulatedSince = DateTime.Now;
        _checkedAt      = default;
    }
#endif

    /// <summary>Le nom d'un thème dans la langue du plugin.</summary>
    public static string Name(Season season)
    {
        var l = Plugin.L;
        return season switch
        {
            Season.NewYear      => l.SeasonNewYear,
            Season.Valentine    => l.SeasonValentine,
            Season.Easter       => l.SeasonEaster,
            Season.Halloween    => l.SeasonHalloween,
            Season.Christmas    => l.SeasonChristmas,
            Season.LittleLadies => l.SeasonLittleLadies,
            Season.GoldSaucer   => l.SeasonGoldSaucer,
            Season.Moonfire     => l.SeasonMoonfire,
            Season.Rising       => l.SeasonRising,
            _                   => l.SeasonGoldenNight,
        };
    }

    /// <summary>Branché sur <c>UiBuilder.Draw</c>, avant le système de fenêtres.</summary>
    public static void Update()
    {
        SeasonFeed.Tick();

        var now = Now;
        if (now - _checkedAt >= TimeSpan.FromMinutes(1) || now < _checkedAt || _version != SeasonCalendar.Version)
        {
            _checkedAt = now;
            _version   = SeasonCalendar.Version;
            _dated     = SeasonCalendar.For(now);
        }

        var auto = Plugin.Config.SeasonalThemes;
        Current    = auto ? _dated : Season.None;
        AprilFools = auto && SeasonCalendar.IsAprilFools(now);

        if (Current == _applied) return;

        Theme.Use(SeasonPalettes.For(Current));
        _applied = Current;
    }
}
