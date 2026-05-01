using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace EorzeaEventsPlugin.Windows;

public class EstabDetailWindow : Window, IDisposable
{
    private readonly Configuration              _config;
    private readonly HttpClient                 _http = new();
    private          EstablishmentDto?          _estab;
    private          Task<IDalamudTextureWrap?>? _bannerTask;
    private          string                     _copiedKey   = string.Empty;
    private          DateTime                   _copiedUntil = DateTime.MinValue;
    private readonly HashSet<int>               _revealed    = new();

    public EstabDetailWindow(Configuration config)
        : base("##estabdetail", ImGuiWindowFlags.None)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(460, 320),
            MaximumSize = new Vector2(800, 720),
        };
        _config = config;
    }

    public void Open(EstablishmentSummaryDto summary)
    {
        // Affiche immédiatement les infos partielles, puis fetche la fiche complète en arrière-plan
        var partial = new EstablishmentDto
        {
            Id          = summary.Id,
            Name        = summary.Name,
            Slug        = summary.Slug,
            Banner      = summary.Banner,
            Server      = summary.Server,
            District    = summary.District,
            Ward        = summary.Ward,
            Plot        = summary.Plot,
            HousingType = summary.HousingType,
        };
        Open(partial);
        _ = FetchFullAsync(summary.Id);
    }

    private async Task FetchFullAsync(string id)
    {
        var full = await Plugin.Api.GetEstablishmentByIdAsync(id);
        if (full != null && _estab?.Id == id)
            Open(full);
    }

    public void Open(EstablishmentDto estab)
    {
        if (_estab?.Id != estab.Id)
        {
            if (_bannerTask?.IsCompletedSuccessfully == true)
                _bannerTask.Result?.Dispose();
            _bannerTask  = string.IsNullOrEmpty(estab.Banner) ? null : LoadBannerAsync(estab.Banner);
            _revealed.Clear();
        }
        _estab      = estab;
        WindowName  = estab.Name + "##estabdetail";
        IsOpen      = true;
    }

    private async Task<IDalamudTextureWrap?> LoadBannerAsync(string url)
    {
        try
        {
            var bytes = await _http.GetByteArrayAsync(url);
            return await Plugin.TextureProvider.CreateFromImageAsync(
                new ReadOnlyMemory<byte>(bytes), null, default);
        }
        catch { return null; }
    }

    public override void Draw()
    {
        if (_estab == null) return;
        var l = Plugin.L;

        // ── Bannière ──────────────────────────────────────────────────────────
        var wrap = _bannerTask?.IsCompletedSuccessfully == true ? _bannerTask.Result : null;
        if (wrap != null) DrawBanner(wrap);

        // ── Nom ───────────────────────────────────────────────────────────────
        ImGui.TextColored(UiStyle.TextTitle, _estab.Name);
        ImGui.Spacing();

        // ── Description ───────────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(_estab.Description))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextColored(UiStyle.TextMuted, _estab.Description);
            ImGui.PopTextWrapPos();
            ImGui.Spacing();
        }

        ImGui.Separator();
        ImGui.Spacing();

        // ── Localisation ──────────────────────────────────────────────────────
        bool hasLocation = !string.IsNullOrEmpty(_estab.Datacenter)
                        || !string.IsNullOrEmpty(_estab.Server)
                        || !string.IsNullOrEmpty(_estab.District)
                        || _estab.Ward.HasValue;
        if (hasLocation)
        {
            bool inline = false;
            if (!string.IsNullOrEmpty(_estab.Datacenter))
            { UiPrimitives.DrawChip(_estab.Datacenter!); ImGui.SameLine(0, 4); inline = true; }
            if (!string.IsNullOrEmpty(_estab.Server))
            { UiPrimitives.DrawChip(_estab.Server!); ImGui.SameLine(0, 4); inline = true; }
            if (!string.IsNullOrEmpty(_estab.District))
            { UiPrimitives.DrawChip(DistrictLabel(_estab.District!)); ImGui.SameLine(0, 4); inline = true; }
            if (_estab.Ward.HasValue)
            { UiPrimitives.DrawChip(string.Format(l.HousingWard, _estab.Ward)); ImGui.SameLine(0, 4); inline = true; }
            if (_estab.Plot.HasValue)
            { UiPrimitives.DrawChip(string.Format("{0} {1}", l.FieldPlot, _estab.Plot)); ImGui.SameLine(0, 4); inline = true; }
            if (_estab.ApartmentNumber.HasValue)
            { UiPrimitives.DrawChip(string.Format("{0} {1}", l.FieldRoom, _estab.ApartmentNumber)); ImGui.SameLine(0, 4); inline = true; }
            if (_estab.Wing)
            { UiPrimitives.DrawChip(l.HousingAnnex); inline = false; }
            if (inline) ImGui.NewLine();
            ImGui.Spacing();
        }

        // ── Boutons d'action ──────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(_estab.DiscordInvite))
        {
            if (UiPrimitives.ColorButton(l.EstabDiscord + "##discord", UiStyle.MediumButton,
                UiStyle.PrimaryNormal, UiStyle.PrimaryHovered, UiStyle.PrimaryActive))
                OpenUrl(_estab.DiscordInvite!);
            ImGui.SameLine(0, 4);
        }
        if (!string.IsNullOrEmpty(_estab.Slug))
        {
            if (UiPrimitives.ColorButton(l.EstabOpenSite + "##site", UiStyle.MediumButton,
                UiStyle.SecondaryNormal, UiStyle.SecondaryHovered, UiStyle.SecondaryActive))
                OpenUrl(_config.BaseUrl + "/etablissements/" + _estab.Slug);
        }
        ImGui.Spacing();

        // ── Syncshells ────────────────────────────────────────────────────────
        List<SyncshellEntryDto>? syncshells = null;
        try { syncshells = JsonSerializer.Deserialize<List<SyncshellEntryDto>>(_estab.Syncshells); } catch { }
        if (syncshells?.Count > 0)
        {
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextColored(UiStyle.TextSection, l.EstabSyncshells.ToUpper());
            ImGui.Spacing();

            bool copyExpired = DateTime.UtcNow >= _copiedUntil;

            for (int i = 0; i < syncshells.Count; i++)
            {
                var s     = syncshells[i];
                var label = SyncshellLabel(s);

                // Label du type (Snowcloak, Glamourer, etc.)
                ImGui.TextColored(UiStyle.TextMuted, label);
                ImGui.SameLine(0, 8);

                // ID avec bouton copier
                var idKey     = $"id_{i}";
                bool idCopied = !copyExpired && _copiedKey == idKey;
                ImGui.Text(s.Id);
                ImGui.SameLine(0, 6);
                if (ImGui.SmallButton((idCopied ? l.EstabCopied : "ID") + "##cpid_" + i))
                {
                    ImGui.SetClipboardText(s.Id);
                    _copiedKey   = idKey;
                    _copiedUntil = DateTime.UtcNow.AddSeconds(2);
                }

                // Mot de passe (si présent)
                if (!string.IsNullOrEmpty(s.Password))
                {
                    bool revealed = _revealed.Contains(i);
                    var pwKey     = $"pw_{i}";
                    bool pwCopied = !copyExpired && _copiedKey == pwKey;

                    ImGui.SameLine(0, 12);
                    ImGui.TextColored(UiStyle.TextSubtle, l.EstabPassword + ":");
                    ImGui.SameLine(0, 4);

                    if (revealed)
                        ImGui.Text(s.Password);
                    else
                        ImGui.TextColored(UiStyle.TextSubtle, new string('•', Math.Min(s.Password.Length, 12)));

                    ImGui.SameLine(0, 6);
                    if (ImGui.SmallButton((revealed ? l.Hide : l.EstabReveal) + "##rev_" + i))
                    {
                        if (revealed) _revealed.Remove(i); else _revealed.Add(i);
                    }
                    ImGui.SameLine(0, 4);
                    if (ImGui.SmallButton((pwCopied ? l.EstabCopied : "MdP") + "##cppw_" + i))
                    {
                        ImGui.SetClipboardText(s.Password);
                        _copiedKey   = pwKey;
                        _copiedUntil = DateTime.UtcNow.AddSeconds(2);
                    }
                }

                ImGui.Dummy(new Vector2(0, 2));
            }
        }
    }

    private void DrawBanner(IDalamudTextureWrap wrap)
    {
        const float MaxH  = 160f;
        var avail         = ImGui.GetContentRegionAvail().X;
        var imgAspect     = wrap.Width / (float)wrap.Height;
        var cardAspect    = avail / MaxH;

        float u0, v0, u1, v1;
        if (imgAspect >= cardAspect)
        {
            var uRange = cardAspect / imgAspect;
            u0 = (1f - uRange) / 2f; u1 = 1f - u0;
            v0 = 0f; v1 = 1f;
        }
        else
        {
            var vRange = imgAspect / cardAspect;
            v0 = (1f - vRange) / 2f; v1 = 1f - v0;
            u0 = 0f; u1 = 1f;
        }

        ImGui.Image(wrap.Handle, new Vector2(avail, MaxH), new Vector2(u0, v0), new Vector2(u1, v1));
        ImGui.Spacing();
    }

    private static string SyncshellLabel(SyncshellEntryDto s) => s.Type switch
    {
        "snowcloak"  => "Snowcloak",
        "lightless"  => "Lightless",
        "glamourer"  => "Glamourer",
        "umbra"      => "Umbra",
        "mare"       => "Mare Synchronos",
        "lightsync"  => "Lightsync",
        "autre"      => !string.IsNullOrEmpty(s.Name) ? s.Name : "Autre",
        var other    => other,
    };

    private static string DistrictLabel(string key) =>
        Plugin.L.DistrictLabels.TryGetValue(key.ToLowerInvariant(), out var v) ? v : key;

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    public void Dispose()
    {
        if (_bannerTask?.IsCompletedSuccessfully == true)
            _bannerTask.Result?.Dispose();
        _http.Dispose();
    }
}
