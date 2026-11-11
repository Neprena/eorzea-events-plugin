using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System.Numerics;
using System.Threading.Tasks;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Confirmation avant de bloquer un joueur depuis le menu contextuel.
///
/// Le blocage retire relations et accès amis RP, sans retour possible : un clic
/// dans un menu ne suffit pas, la conséquence doit être lue avant de valider.
/// </summary>
public sealed class BlockCharacterWindow : ThemedWindow
{
    private ulong  _contentId;
    private string _name  = string.Empty;
    private string _world = string.Empty;

    /// <summary>Un appel est en cours : un second clic ne doit pas le doubler.</summary>
    private bool _busy;

    private bool _placePending;

    private static readonly Vector2 OpenSize = new(460f, 250f);

    public BlockCharacterWindow()
        : base("##blockcharacter", ImGuiWindowFlags.NoCollapse)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380f, 200f),
            MaximumSize = new Vector2(800f, 600f),
        };
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (!_placePending) return;
        _placePending = false;

        var viewport = ImGui.GetMainViewport();
        var size     = Theme.S(OpenSize.X, OpenSize.Y);

        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowPos(viewport.WorkPos + (viewport.WorkSize - size) * 0.5f,
                               ImGuiCond.Always);
    }

    public void Open(ulong contentId, string name, string world)
    {
        _contentId    = contentId;
        _name         = name;
        _world        = world;
        _busy         = false;
        WindowName    = $"{string.Format(Plugin.L.BlockConfirmTitle, name)}##blockcharacter";
        _placePending = true;
        IsOpen        = true;
    }

    protected override void DrawContents()
    {
        var l = Plugin.L;

        Layout.SectionHeader(string.Format(l.BlockConfirmTitle, _name), Icons.Blocked);
        Text.Wrapped(string.Format(l.BlockConfirmBody, _name));

        Layout.Divider(Theme.GapS);

        if (Btn.Draw(l.BlockConfirmAction, BtnTone.Danger, BtnSize.Medium,
                     disabled: _busy, id: "block_confirm"))
            Confirm();

        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        if (Btn.Draw(l.Cancel, BtnTone.Ghost, BtnSize.Medium, id: "block_cancel"))
            IsOpen = false;
    }

    private void Confirm()
    {
        _busy = true;

        var contentId = _contentId;
        var name      = _name;
        var world     = _world;
        var hash      = Plugin.HashContentId(contentId);

        Task.Run(async () =>
        {
            var result = await Plugin.Api.BlockCharacterAsync(hash, name, world);
            await Plugin.Framework.RunOnFrameworkThread(() =>
            {
                var l = Plugin.L;
                switch (result)
                {
                    case ApiClient.BlockCharacterResult.Blocked:
                        Plugin.ChatGui.Print($"[Eorzea Events] {string.Format(l.BlockDone, name)}");
                        // Le serveur vient de retirer amitiés et demandes : relire pour ne plus les afficher.
                        Plugin.RefreshFriends(force: true);
                        Plugin.RequestRelationRequestsRefresh();
                        break;
                    case ApiClient.BlockCharacterResult.LimitReached:
                        Plugin.ChatGui.PrintError($"[Eorzea Events] {l.BlockLimit}");
                        break;
                    default:
                        Plugin.ChatGui.PrintError($"[Eorzea Events] {ServerNotice.Explain(l.BlockFailed)}");
                        break;
                }

                Plugin.RefreshCharacterBlocks();
                _busy  = false;
                IsOpen = false;
            });
        });
    }
}
