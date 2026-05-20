using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

public class SetupWindow : Window
{
    private readonly Configuration            _config;
    private          ISharedImmediateTexture? _banner;
    private int    _step         = 0;
    private bool   _tokenInvalid = false;
    private bool   _isMigration  = false;
    private int    _initialCount = 0;

    public void Restart(bool tokenInvalid = false, bool migration = false)
    {
        _step         = (tokenInvalid || migration) ? 1 : 0;
        _tokenInvalid = tokenInvalid;
        _isMigration  = migration;
        _initialCount = _config.CharacterTokens.Count;
        IsOpen        = true;
    }

    public SetupWindow(Configuration config)
        : base("Eorzea Events — Configuration##setup", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(620, 400),
            MaximumSize = new Vector2(900, 640),
        };
        _config = config;

        var bannerFile = new FileInfo(
            Path.Combine(Plugin.PluginInterface.AssemblyLocation.DirectoryName!, "banner.png"));
        if (bannerFile.Exists)
            _banner = Plugin.TextureProvider.GetFromFile(bannerFile);
    }

    public override void Draw()
    {
        // Auto-advance vers Done : nouveau perso OU re-link d'un perso existant.
        if (_step == 1 && (_config.CharacterTokens.Count > _initialCount
            || Plugin.ActiveLinkState?.Status == "bound"))
            _step = 2;

        switch (_step)
        {
            case 0: DrawWelcome(); break;
            case 1: DrawLink();    break;
            case 2: DrawDone();    break;
        }
    }

    private void DrawBanner(float maxHeight = 120f)
    {
        if (_banner == null) return;
        IDalamudTextureWrap? wrap = _banner.GetWrapOrDefault();
        if (wrap == null) return;

        var availW  = ImGui.GetContentRegionAvail().X;
        var aspect  = wrap.Width / (float)wrap.Height;
        var h       = Math.Min(availW / aspect, maxHeight);
        var w       = h * aspect;
        var offsetX = (availW - w) / 2f;
        if (offsetX > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);
        ImGui.Image(wrap.Handle, new Vector2(w, h));
        ImGui.Spacing();
    }

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    // ─── Étape 0 : Bienvenue ──────────────────────────────────────────────────

    private void DrawWelcome()
    {
        var l = Plugin.L;
        DrawBanner();

        ImGui.Text(l.SetupWelcomeL1);
        ImGui.SameLine(0, 4);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.7f, 1f, 1f));
        ImGui.Text("eorzea.events");
        if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsItemClicked()) OpenUrl("https://eorzea.events");
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(UiStyle.TextMuted, l.SetupWelcomeL2);
        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextMuted, l.SetupWelcomeL3);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (UiPrimitives.ColorButton(l.SetupStart, UiStyle.MediumButton,
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
            _step = 1;
    }

    // ─── Étape 1 : Couplage du personnage in-game ────────────────────────────

    private void DrawLink()
    {
        var l = Plugin.L;
        DrawBanner(80f);

        // Bannières contextuelles (au plus une à la fois)
        if (_isMigration)
            UiPrimitives.DrawAlert(new Vector4(0.3f, 0.7f, 1f, 1f),
                "✦  " + l.SetupMigrationTitle, l.SetupMigrationDesc, () => { });
        else if (_tokenInvalid)
            UiPrimitives.DrawAlert(new Vector4(1f, 0.7f, 0.2f, 1f),
                "⚠  " + l.SetupTokenInvalid, string.Empty, () => { });

        ImGui.Spacing();
        ImGui.TextColored(UiStyle.TextSection, l.SetupStepTitle.ToUpper());
        ImGui.Spacing();
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(UiStyle.TextMuted, l.SetupStepDesc);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var link = Plugin.ActiveLinkState;
        var couplingInProgress = link != null
            && link.Status == "pending"
            && DateTime.UtcNow < link.ExpiresAt;

        if (couplingInProgress)
        {
            ImGui.TextColored(UiStyle.TextSection, "EN ATTENTE DE CONFIRMATION");
            ImGui.Spacing();
            ImGui.PushTextWrapPos(0);
            ImGui.TextColored(UiStyle.TextMuted, $"{link!.CharacterName} @ {link.WorldName}");
            ImGui.Spacing();
            ImGui.TextColored(UiStyle.TextSubtle,
                "Une page de confirmation est ouverte dans votre navigateur. " +
                "Connectez-vous au site si nécessaire puis cliquez « Confirmer ».");
            if (!string.IsNullOrEmpty(link.ErrorMessage))
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "⚠  " + link.ErrorMessage);
            }
            ImGui.PopTextWrapPos();
            ImGui.Spacing();
            if (UiPrimitives.ColorButton("Rouvrir la page de confirmation", new Vector2(-1, 0),
                UiStyle.SecondaryNormal, UiStyle.SecondaryHovered, UiStyle.SecondaryActive))
            {
                try { Dalamud.Utility.Util.OpenLink(link.LinkUrl); } catch { /* ignore */ }
            }
        }
        else if (link != null && (link.Status == "expired" || link.Status == "error"))
        {
            ImGui.PushTextWrapPos(0);
            ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f),
                link.Status == "expired"
                    ? "⏱  Session expirée. Relancez la procédure."
                    : "✗  Échec du couplage. Relancez la procédure.");
            ImGui.PopTextWrapPos();
            ImGui.Spacing();
            if (UiPrimitives.ColorButton("Réessayer", new Vector2(-1, 0),
                UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
            {
                _initialCount = _config.CharacterTokens.Count;
                _ = Plugin.StartCharacterLinkAsync();
            }
        }
        else
        {
            ImGui.TextColored(UiStyle.TextMuted, l.SetupTokenLabel);
            ImGui.Spacing();

            var player = Plugin.ObjectTable.LocalPlayer;
            if (player == null)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), l.SetupErrPrefix);
                ImGui.Spacing();
                ImGui.BeginDisabled();
                UiPrimitives.ColorButton("Lier ce personnage", new Vector2(-1, 0),
                    UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive);
                ImGui.EndDisabled();
            }
            else
            {
                var name      = player.Name.TextValue;
                var worldName = player.HomeWorld.Value.Name.ToString();
                var worldId   = (int)player.HomeWorld.RowId;
                ImGui.TextColored(UiStyle.TextTitle, $"{name} @ {worldName}");

                var existing = _config.FindCharacterToken(name, worldId);
                if (existing != null)
                {
                    ImGui.Spacing();
                    ImGui.TextColored(UiStyle.StatusOpen,
                        "✓ Ce personnage est déjà lié. Vous pouvez fermer cet assistant ou re-lier pour générer un nouveau token.");
                }

                ImGui.Spacing();
                var label = existing != null ? "Re-lier ce personnage" : "Lier ce personnage";
                if (UiPrimitives.ColorButton(label, new Vector2(-1, 0),
                    UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                {
                    _initialCount = _config.CharacterTokens.Count;
                    _ = Plugin.StartCharacterLinkAsync();
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_isMigration)
        {
            if (ImGui.Button("Ignorer pour l'instant##migskip", UiStyle.SmallButton))
            {
                _config.MigrationNoticeSeen = true;
                _config.Save();
                IsOpen = false;
                Plugin.OpenMain();
            }
        }
        else
        {
            if (ImGui.Button(l.SetupSkip, UiStyle.SmallButton))
            {
                IsOpen = false;
                Plugin.OpenMain();
            }
        }
    }

    // ─── Étape 2 : Terminé ────────────────────────────────────────────────────

    private void DrawDone()
    {
        var l = Plugin.L;
        DrawBanner();

        // Si on vient d'une migration, marquer comme vu dès que le link est réussi.
        if (_isMigration && !_config.MigrationNoticeSeen)
        {
            _config.MigrationNoticeSeen = true;
            _config.Save();
        }

        UiPrimitives.DrawCard(() =>
        {
            ImGui.TextColored(UiStyle.StatusOpen, l.SetupDoneTitle);
            ImGui.Spacing();
            ImGui.PushTextWrapPos(0);
            ImGui.TextColored(UiStyle.TextMuted, l.SetupDoneL1);
            ImGui.TextColored(UiStyle.TextMuted, l.SetupDoneL2);
            ImGui.Spacing();
            ImGui.TextColored(UiStyle.TextSubtle, l.SetupDoneHint);
            ImGui.PopTextWrapPos();
        });

        ImGui.Spacing();
        if (UiPrimitives.ColorButton(l.SetupOpenPlugin, UiStyle.WideButton,
            UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
        {
            IsOpen = false;
            Plugin.OpenMain();
        }
    }
}
