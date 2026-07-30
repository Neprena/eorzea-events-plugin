using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Ui.Shell;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// One-shot announcement shown once after the RP Profile &amp; Availability feature ships.
/// Opens automatically on first launch if the user has a configured API token.
/// Dismissed permanently by clicking either button.
/// </summary>
public class RpAnnouncementWindow : ThemedWindow
{
    private readonly Configuration _config;

    public RpAnnouncementWindow(Configuration config)
        : base("##rpannouncement",
               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 320),
            MaximumSize = new Vector2(520, 320),
        };
        _config = config;
    }

    public override void OnOpen()
    {
        WindowName = $"{Plugin.L.AnnouncementTitle}##rpannouncement";
    }

    public override void Draw()
    {
        var l = Plugin.L;

        // Badge coloré
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.84f, 0f, 1f));
        ImGui.TextUnformatted(l.AnnouncementBadge);
        ImGui.PopStyleColor();

        ImGui.Spacing();

        // Corps du message
        ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);
        ImGui.TextUnformatted(l.AnnouncementBody);
        ImGui.PopTextWrapPos();

        ImGui.Spacing();

        // Note de bas de page
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.55f, 0.55f, 0.55f, 1f));
        ImGui.TextUnformatted(l.AnnouncementIndicator);
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (UiPrimitives.ColorButton(l.AnnouncementConfigure, Vector2.Zero,
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
        {
            Dismiss();
            Plugin.OpenRpProfileWizard();
        }

        ImGui.SameLine();

        if (ImGui.Button(l.AnnouncementLater, UiStyle.SmallButton))
            Dismiss();
    }

    private void Dismiss()
    {
        _config.RpAnnouncementSeen = true;
        _config.Save();
        IsOpen = false;
    }
}
