using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using EorzeaEventsPlugin.Api;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace EorzeaEventsPlugin.Windows;

public class EstabDetailWindow : ThemedWindow, IDisposable
{
    private readonly Configuration              _config;
    private          EstablishmentDto?          _estab;
    private          string                     _copiedKey   = string.Empty;
    private          DateTime                   _copiedUntil = DateTime.MinValue;
    private readonly HashSet<int>               _revealed    = new();

    public EstabDetailWindow(Configuration config)
        : base("##estabdetail", ImGuiWindowFlags.None)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
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
        if (_estab?.Id != estab.Id) _revealed.Clear();

        _estab      = estab;
        WindowName  = Glyphs.Safe(estab.Name) + "##estabdetail";
        IsOpen      = true;
    }

    protected override void DrawContents()
    {
        if (_estab == null) return;
        var l = Plugin.L;

        // Le cache partagé garde la bannière d'une ouverture à l'autre : rouvrir
        // une fiche déjà consultée n'entraîne plus de retéléchargement.
        Cover.Draw(Textures.Get(_estab.Banner));

        // L'avertissement de contenu figurait sur la carte de la liste et
        // disparaissait ici, sur l'écran le plus détaillé : c'est précisément
        // celui où il doit rester. Mêmes puces que la carte, qui ne signale que
        // le semi-RP : le RP complet est l'attendu, il n'a pas de libellé.
        // En tête, comme sur les autres fiches : ce qui change la décision d'y
        // aller se lit avant le nom.
        var badge = false;
        if (_estab.RpType == "semi_rp")
        {
            Chip.Draw(l.EstabSemiRp, ChipTone.Warning);
            if (_estab.IsNsfw) ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            badge = true;
        }
        if (_estab.IsNsfw) { Chip.Draw("18+", ChipTone.Danger); badge = true; }
        if (badge) Layout.Spacer(Theme.GapXs);

        // Teinté de la couleur choisie par le gérant, comme sur sa carte.
        var accent = Theme.TryParseHex(_estab.AccentColor) is { } custom
            ? Theme.EnsureReadable(custom)
            : Theme.Accent;
        Text.TitleWrapped(_estab.Name, accent);

        DrawLocation(l);

        if (!string.IsNullOrEmpty(_estab.Description))
        {
            Layout.Divider(Theme.GapS);
            MarkdownView.Draw(_estab.Description);
        }

        DrawSyncshells(l);

        // ── Actions ───────────────────────────────────────────────────────────
        Layout.Divider(Theme.GapM);

        var first = true;
        if (!string.IsNullOrEmpty(_estab.DiscordInvite))
        {
            if (Btn.Draw(l.EstabDiscord, BtnTone.Primary, BtnSize.Medium, Icons.Language, id: "estab_discord"))
                OpenUrl(NormalizeDiscordInvite(_estab.DiscordInvite!));
            first = false;
        }
        if (!string.IsNullOrEmpty(_estab.Slug))
        {
            if (!first) ImGui.SameLine(0f, Theme.S(Theme.GapS));
            if (Btn.Draw(l.EstabOpenSite, BtnTone.Secondary, BtnSize.Medium, Icons.External, id: "estab_site"))
                OpenUrl(_config.BaseUrl + "/etablissements/" + _estab.Slug);
            first = false;
        }

        // La fiche complète est la seule à connaître l'aile et le numéro
        // d'appartement, donc le seul endroit où un voyage vers un appartement
        // peut être proposé sans deviner.
        TravelButton.Draw(_estab, $"estab_{_estab.Id}", sameLine: !first);
    }

    /// <summary>Adresse en pastilles, sous le nom : où se trouve le lieu.</summary>
    private void DrawLocation(Loc l)
    {
        var parts = new List<(string Text, Dalamud.Interface.FontAwesomeIcon? Icon)>();
        if (!string.IsNullOrEmpty(_estab!.Datacenter)) parts.Add((_estab.Datacenter!, null));
        if (!string.IsNullOrEmpty(_estab.Server))      parts.Add((_estab.Server!, Icons.World));
        if (!string.IsNullOrEmpty(_estab.District))    parts.Add((DistrictLabel(_estab.District!), Icons.Location));
        if (_estab.Ward.HasValue)            parts.Add((string.Format(l.HousingWard, _estab.Ward), Icons.Housing));
        if (_estab.Plot.HasValue)            parts.Add(($"{l.FieldPlot} {_estab.Plot}", null));
        if (_estab.ApartmentNumber.HasValue) parts.Add(($"{l.FieldRoom} {_estab.ApartmentNumber}", null));
        if (_estab.Wing)                     parts.Add((l.HousingAnnex, null));
        if (parts.Count == 0) return;

        Layout.Spacer(Theme.GapXs);
        var flow = FlowRow.Fill(Theme.S(Theme.GapXs));
        foreach (var (text, icon) in parts)
        {
            flow.Next(Chip.Measure(text, icon));
            Chip.Draw(text, ChipTone.Neutral, icon);
        }
    }

    /// <summary>Codes de synchronisation, à copier pour rejoindre le lieu.</summary>
    private void DrawSyncshells(Loc l)
    {
        List<SyncshellEntryDto>? syncshells = null;
        try { syncshells = JsonSerializer.Deserialize<List<SyncshellEntryDto>>(_estab!.Syncshells); } catch { }
        if (syncshells is not { Count: > 0 }) return;

        Layout.Spacer(Theme.GapM);
        Layout.SectionHeader(l.EstabSyncshells, Icons.Copy);

        var copyExpired = DateTime.UtcNow >= _copiedUntil;
        var gap         = Theme.S(Theme.GapXs);

        for (var i = 0; i < syncshells.Count; i++)
        {
            var s = syncshells[i];

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(Theme.TextMuted, SyncshellLabel(s));
            ImGui.SameLine(0f, Theme.S(Theme.GapM));
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(Theme.Text, s.Id);
            ImGui.SameLine(0f, gap);

            var idKey = $"id_{i}";
            if (Btn.Draw(!copyExpired && _copiedKey == idKey ? l.EstabCopied : "ID",
                         BtnTone.Ghost, BtnSize.Small, Icons.Copy, id: $"cpid_{i}"))
                Copy(s.Id, idKey);

            if (!string.IsNullOrEmpty(s.Password))
            {
                var revealed = _revealed.Contains(i);
                var pwKey    = $"pw_{i}";

                ImGui.SameLine(0f, Theme.S(Theme.GapL));
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Theme.TextFaint, l.EstabPassword + " :");
                ImGui.SameLine(0f, gap);
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(revealed ? Theme.Text : Theme.TextFaint,
                                  revealed ? s.Password : new string('•', Math.Min(s.Password.Length, 12)));
                ImGui.SameLine(0f, gap);

                if (Btn.Draw(revealed ? l.Hide : l.EstabReveal, BtnTone.Ghost, BtnSize.Small,
                             revealed ? Icons.Hide : Icons.Show, id: $"rev_{i}"))
                {
                    if (revealed) _revealed.Remove(i); else _revealed.Add(i);
                }
                ImGui.SameLine(0f, gap);
                if (Btn.Draw(!copyExpired && _copiedKey == pwKey ? l.EstabCopied : "MdP",
                             BtnTone.Ghost, BtnSize.Small, Icons.Copy, id: $"cppw_{i}"))
                    Copy(s.Password, pwKey);
            }
        }
    }

    private void Copy(string value, string key)
    {
        ImGui.SetClipboardText(value);
        _copiedKey   = key;
        _copiedUntil = DateTime.UtcNow.AddSeconds(2);
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

    private static string NormalizeDiscordInvite(string invite) =>
        invite.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? invite
            : "https://discord.gg/" + invite;

    private static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

    /// <summary>
    /// Les images appartiennent désormais à <see cref="Textures"/> : cette
    /// fenêtre ne détient plus de ressource à libérer.
    /// </summary>
    public void Dispose() => GC.SuppressFinalize(this);
}
