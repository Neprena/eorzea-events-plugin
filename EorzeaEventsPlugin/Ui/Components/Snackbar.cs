using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Notification brève en bas de la fenêtre principale, avec une action
/// facultative, le plus souvent « Annuler ».
///
/// Une seule à la fois ; les suivantes attendent leur tour. L'échéance se
/// compte en temps réel et se traite dans la boucle du plugin
/// (<see cref="Update"/>), pas au rendu : fermer la fenêtre ne doit pas
/// suspendre un retrait différé, qui resterait sinon en suspens jusqu'à la
/// réouverture.
/// </summary>
internal static class Snackbar
{
    private const float DefaultSeconds = 7f;

    private sealed class Entry
    {
        public required string  Message   { get; init; }
        public          string? Action    { get; init; }
        public          Action? OnAction  { get; init; }
        public          Action? OnExpire  { get; init; }
        public required float   Seconds   { get; init; }

        /// <summary>Échéance, fixée à l'affichage et non à la mise en file.</summary>
        public DateTime ExpiresAt { get; set; }
    }

    private static readonly Queue<Entry> Pending = [];
    private static Entry? _current;

    /// <summary>Dernière notification affichée, gardée le temps de son départ en fondu.</summary>
    private static Entry? _shown;

    /// <summary>
    /// Met une notification en file.
    /// </summary>
    /// <param name="onAction">Appelé au clic sur l'action. L'échéance n'est alors pas appelée.</param>
    /// <param name="onExpire">
    /// Appelé quand la notification s'en va sans que l'action ait été choisie :
    /// c'est là qu'un geste différé, comme un retrait, s'exécute pour de bon.
    /// </param>
    public static void Show(string message, string? action = null, Action? onAction = null,
                            Action? onExpire = null, float seconds = DefaultSeconds)
    {
        Pending.Enqueue(new Entry
        {
            Message  = message,
            Action   = action,
            OnAction = onAction,
            OnExpire = onExpire,
            Seconds  = seconds,
        });

        if (_current == null) Advance();
    }

    /// <summary>
    /// Échéances. À appeler depuis la boucle du plugin, sur le thread du jeu.
    /// </summary>
    public static void Update()
    {
        if (_current == null || DateTime.UtcNow < _current.ExpiresAt) return;

        var expired = _current;
        Advance();
        expired.OnExpire?.Invoke();
    }

    /// <summary>
    /// Exécute toutes les échéances en attente, au déchargement du plugin : un
    /// retrait demandé ne doit pas se perdre parce que le jeu se ferme.
    /// </summary>
    public static void Flush()
    {
        var all = new List<Entry>();
        if (_current != null) all.Add(_current);
        all.AddRange(Pending);

        _current = null;
        Pending.Clear();

        foreach (var entry in all) entry.OnExpire?.Invoke();
    }

    private static void Advance()
    {
        _current = Pending.TryDequeue(out var next) ? next : null;
        if (_current != null) _current.ExpiresAt = DateTime.UtcNow.AddSeconds(_current.Seconds);
    }

    /// <summary>
    /// Dessine la notification courante au pied de la zone donnée, en
    /// coordonnées écran.
    ///
    /// Dans une fenêtre enfant créée après le contenu : ImGui la place alors
    /// au-dessus, pour l'affichage comme pour la souris, si bien qu'un clic sur
    /// « Annuler » ne traverse pas jusqu'au bouton de la page en dessous.
    /// </summary>
    public static void Draw(Vector2 areaMin, Vector2 areaMax)
    {
        // Glissement et fondu à l'apparition comme au départ : la valeur suit
        // la présence d'une notification, pas l'instant de son arrivée.
        var shown = Motion.Approach("##snackbar_shown", _current != null ? 1f : 0f, Motion.Emphasised);
        if (_current != null) _shown = _current;
        if (_shown == null || shown <= 0.01f) return;

        // En partance, elle ne répond plus : son action a déjà été jouée, ou
        // son délai est échu.
        var entry   = _shown;
        var leaving = _current == null;
        var padX   = Theme.S(14f);
        var height = Theme.S(46f);
        var gap    = Theme.S(Theme.GapL);

        var textSize    = ImGui.CalcTextSize(entry.Message);
        var actionWidth = entry.Action != null ? ImGui.CalcTextSize(entry.Action).X + Theme.S(24f) : 0f;
        var iconWidth   = Theme.S(24f);
        var width       = MathF.Min(areaMax.X - areaMin.X - Theme.S(32f),
                                    padX + iconWidth + textSize.X + gap + actionWidth + Theme.S(8f));

        var min = new Vector2(MathF.Round(areaMin.X + (areaMax.X - areaMin.X - width) * 0.5f),
                              MathF.Round(areaMax.Y - Theme.S(18f) - height + (1f - shown) * Theme.S(24f)));
        var max = min + new Vector2(width, height);

        ImGui.SetCursorScreenPos(min);

        // L'opacité passe par les couleurs, une à une : poussée aussi en style,
        // elle s'appliquerait deux fois.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var child = ImRaii.Child("##snackbar", max - min, false,
                                       ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
                                       | ImGuiWindowFlags.NoBackground);
        if (!child) return;

        var dl = ImGui.GetWindowDrawList();
        var r  = Theme.S(Theme.RadiusCard);

        // Opaque, contrairement aux cartes : posée sur un décor clair, une
        // notification translucide perdrait son texte.
        Surface.Panel(dl, min, max, Theme.Alpha(Theme.BgSurface, shown),
                      Theme.Faded(Theme.BorderLight, shown), r, shadowSpread: 10f);

        var mid = min.Y + height * 0.5f;

        using (ImRaii.PushColor(ImGuiCol.Text, Theme.Online))
        {
            var glyph = Icons.Check.S();
            var size  = ImGui.CalcTextSize(glyph);
            dl.AddText(new Vector2(min.X + padX, mid - size.Y * 0.5f),
                       ImGui.GetColorU32(Theme.Alpha(Theme.Online, shown)), glyph);
        }

        dl.AddText(new Vector2(min.X + padX + iconWidth, mid - textSize.Y * 0.5f),
                   ImGui.GetColorU32(Theme.Alpha(Theme.Text, shown)), entry.Message);

        if (entry.Action == null || leaving) return;

        var buttonMin = new Vector2(max.X - Theme.S(8f) - actionWidth, min.Y + Theme.S(8f));
        var buttonMax = new Vector2(max.X - Theme.S(8f), max.Y - Theme.S(8f));

        ImGui.SetCursorScreenPos(buttonMin);
        var clicked = ImGui.InvisibleButton("##snackbar_action", buttonMax - buttonMin);
        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        dl.AddRectFilled(buttonMin, buttonMax,
                         ImGui.GetColorU32(Theme.Alpha(Theme.Accent, (hovered ? 0.24f : 0.14f) * shown)),
                         Theme.S(Theme.RadiusFrame));

        var actionSize = ImGui.CalcTextSize(entry.Action);
        dl.AddText(buttonMin + (buttonMax - buttonMin - actionSize) * 0.5f,
                   ImGui.GetColorU32(Theme.Alpha(Theme.Link, shown)), entry.Action);

        if (!clicked) return;

        Advance();
        entry.OnAction?.Invoke();
    }
}
