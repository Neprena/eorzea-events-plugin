using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Portrait RP en grand, façon lightbox : centré, sans chrome, refermé au premier
/// clic ou par Échap.
///
/// Une fenêtre plutôt qu'un popup ImGui. Un popup se referme dès qu'une frame ne
/// le redessine pas, or le portrait s'ouvre depuis des cartes qui disparaissent au
/// fil de l'eau : une recherche dans « Autour de moi », un rafraîchissement de la
/// liste des joueurs disponibles ou un changement de page suffiraient à le faire
/// sauter. La fenêtre, elle, vit sa vie, et Dalamud lui offre Échap sans code.
///
/// Aucune ressource à libérer : la texture appartient au cache
/// <see cref="Textures"/>, comme pour les bannières d'établissement.
/// </summary>
public class PortraitZoomWindow : ThemedWindow
{
    private string  _url           = string.Empty;
    private string  _characterName = string.Empty;
    private int     _openedFrame;
    private Vector2 _size = Vector2.Zero;

    public PortraitZoomWindow()
        : base("##portraitzoom",
               ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse
               | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
               | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        // Pas de LogicalSizeConstraints : elles se battraient avec AlwaysAutoResize.
        ShowCloseButton = false;
    }

    public void Open(string portraitUrl, string characterName)
    {
        _url           = portraitUrl;
        _characterName = characterName;
        _openedFrame   = ImGui.GetFrameCount();
        IsOpen         = true;
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (_size == Vector2.Zero) return;

        // Centrage sur l'espace utile, à partir de la taille mesurée à la frame
        // précédente : AlwaysAutoResize ne la donne pas à l'avance.
        var viewport = ImGui.GetMainViewport();
        var window   = _size + Theme.S(Theme.PadWindowX * 2f,
                                       Theme.PadWindowY * 2f + Theme.GapXs + 40f);

        ImGui.SetNextWindowPos(viewport.WorkPos + (viewport.WorkSize - window) * 0.5f);
    }

    public override void Draw()
    {
        var texture = Textures.Get(_url);
        if (texture == null)
        {
            // Le portrait a pu être évincé du cache entre le clic et l'ouverture :
            // il se recharge, inutile de fermer la fenêtre sous le nez du joueur.
            // Dimensions fixes : la fenêtre s'auto-dimensionne, la largeur
            // disponible ne veut donc rien dire tant que rien n'est dessiné.
            Feedback.SkeletonLine(240f, 320f);
            return;
        }

        // Au plus la résolution native (480×640 aujourd'hui) remise à l'échelle de
        // l'interface, et jamais plus que l'espace utile de l'écran : à 200 %,
        // 960×1280 ne tiendrait pas sur un 1080p.
        var viewport = ImGui.GetMainViewport();
        var budget   = viewport.WorkSize * 0.85f;
        var target   = new Vector2(texture.Width, texture.Height) * ImGuiHelpers.GlobalScaleSafe;
        var factor   = MathF.Min(1f, MathF.Min(budget.X / target.X, budget.Y / target.Y));

        _size = target * factor;

        var origin = ImGui.GetCursorScreenPos();
        var dl     = ImGui.GetWindowDrawList();
        var radius = Theme.S(Theme.RadiusCard);

        Surface.Shadow(dl, origin, origin + _size, radius, spread: 10f);
        dl.AddImageRounded(texture.Handle, origin, origin + _size,
                           Vector2.Zero, Vector2.One,
                           ImGui.GetColorU32(Vector4.One), radius);
        dl.AddRect(origin, origin + _size, ImGui.GetColorU32(Theme.Border), radius,
                   ImDrawFlags.None, 1f);

        ImGui.Dummy(_size);

        Layout.Spacer(Theme.GapXs);
        Text.Small(_characterName, Theme.Text);
        Text.Small(Plugin.L.RpProfileZoomClose);

        // Fermeture au premier clic, où qu'il soit : c'est le geste attendu d'une
        // lightbox. Le garde-fou sur la frame d'ouverture évite que le clic qui
        // vient d'ouvrir la fenêtre la referme aussitôt.
        if (ImGui.GetFrameCount() != _openedFrame
            && (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
                || ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
            IsOpen = false;
    }
}
