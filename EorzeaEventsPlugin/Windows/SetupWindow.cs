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
    private int    _step        = 0;
    private string _tokenBuf    = string.Empty;
    private bool   _tokenMasked = true;
    private string _error       = string.Empty;

    public SetupWindow(Configuration config)
        : base("Eorzea Events — Configuration##setup",
               ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
    {
        Size = new Vector2(480, 420);
        SizeCondition = ImGuiCond.Always;
        _config = config;

        var bannerFile = new FileInfo(
            Path.Combine(Plugin.PluginInterface.AssemblyLocation.DirectoryName!, "banner.png"));
        if (bannerFile.Exists)
            _banner = Plugin.TextureProvider.GetFromFile(bannerFile);
    }

    public override void Draw()
    {
        switch (_step)
        {
            case 0: DrawWelcome(); break;
            case 1: DrawToken();   break;
            case 2: DrawDone();    break;
        }
    }

    // ─── Bannière commune ─────────────────────────────────────────────────────

    private void DrawBanner()
    {
        if (_banner == null) return;
        IDalamudTextureWrap? wrap = _banner.GetWrapOrDefault();
        if (wrap == null) return;

        var w = ImGui.GetContentRegionAvail().X;
        var h = Math.Min(w * (wrap.Height / (float)wrap.Width), 160f);
        ImGui.Image(wrap.Handle, new Vector2(w, h));
        ImGui.Spacing();
    }

    // ─── Étape 0 : Bienvenue ─────────────────────────────────────────────────

    private void DrawWelcome()
    {
        DrawBanner();

        ImGui.TextWrapped("Ce plugin vous permet de créer et gérer vos sessions RP");
        ImGui.TextWrapped("directement depuis le jeu, sans quitter FFXIV.");
        ImGui.Spacing();
        ImGui.TextWrapped("La configuration ne prend que quelques secondes.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Commencer", new Vector2(120, 0)))
            _step = 1;
    }

    // ─── Étape 1 : Token API ─────────────────────────────────────────────────

    private void DrawToken()
    {
        DrawBanner();

        ImGui.Text("Étape 1 / 1 — Token API");
        ImGui.Spacing();
        ImGui.TextWrapped("Générez un token API sur votre dashboard, puis collez-le ici.");
        ImGui.Spacing();

        if (ImGui.Button("Ouvrir le dashboard eorzea.events"))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                _config.BaseUrl.TrimEnd('/') + "/dashboard") { UseShellExecute = true });

        ImGui.Spacing();
        ImGui.Text("Token API :");
        ImGui.SetNextItemWidth(-80);
        if (_tokenMasked)
            ImGui.InputText("##token", ref _tokenBuf, 256, ImGuiInputTextFlags.Password);
        else
            ImGui.InputText("##token", ref _tokenBuf, 256);
        ImGui.SameLine();
        if (ImGui.Button(_tokenMasked ? "Afficher" : "Masquer"))
            _tokenMasked = !_tokenMasked;

        if (!string.IsNullOrEmpty(_error))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1, 0.35f, 0.35f, 1), _error);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var canSave = !string.IsNullOrWhiteSpace(_tokenBuf);
        if (!canSave) ImGui.BeginDisabled();
        if (ImGui.Button("Enregistrer", new Vector2(120, 0)))
        {
            var trimmed = _tokenBuf.Trim();
            if (!trimmed.StartsWith("ee_"))
            {
                _error = "Le token doit commencer par « ee_ ».";
            }
            else
            {
                _config.ApiToken = trimmed;
                _config.Save();
                Plugin.RebuildApiClient();
                _step = 2;
                _error = string.Empty;
            }
        }
        if (!canSave) ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button("Passer", new Vector2(80, 0)))
        {
            IsOpen = false;
            Plugin.OpenMain();
        }
    }

    // ─── Étape 2 : Terminé ───────────────────────────────────────────────────

    private void DrawDone()
    {
        DrawBanner();

        ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.5f, 1), "Tout est prêt !");
        ImGui.Spacing();
        ImGui.TextWrapped("Votre token est enregistré. Vous pouvez maintenant créer");
        ImGui.TextWrapped("des sessions RP directement depuis le jeu.");
        ImGui.Spacing();
        ImGui.TextDisabled("Utilisez /eorzea pour ouvrir le panneau principal.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Ouvrir Eorzea Events", new Vector2(160, 0)))
        {
            IsOpen = false;
            Plugin.OpenMain();
        }
    }
}
