using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Nouveautés de la version installée, ouvertes une fois après chaque mise à
/// jour puis acquittées dans la configuration.
///
/// L'ouverture passe par <see cref="PreOpenCheck"/>, évalué à chaque frame,
/// plutôt que par un appel ponctuel au démarrage : le rechargement du plugin en
/// cours de partie et la connexion tardive d'un personnage sont ainsi gérés sans
/// code supplémentaire. C'est le fonctionnement retenu par les plugins de
/// l'écosystème (widget de changelog d'OtterGui, Craftimizer).
/// </summary>
internal sealed class WhatsNewWindow : ThemedWindow
{
    private readonly Configuration _config;

    /// <summary>Réouverture demandée depuis les réglages, hors du cycle automatique.</summary>
    private bool _forceOpen;

    private string _version = string.Empty;

    public WhatsNewWindow(Configuration config)
        : base("##whatsnew", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(620, 520),
            MaximumSize = new Vector2(620, 520),
        };

        // Sortie par le seul bouton d'acquittement : une fermeture par la croix
        // ou par Échap n'enregistrerait rien et ferait revenir la fenêtre au
        // lancement suivant.
        ShowCloseButton     = false;
        RespectCloseHotkey  = false;
        DisableWindowSounds = true;

        _config = config;
    }

    /// <summary>Réouverture manuelle, même si la version a déjà été acquittée.</summary>
    public void Open() => _forceOpen = true;

    /// <summary>Rien à l'écran-titre : les nouveautés attendent l'entrée en jeu.</summary>
    public override bool DrawConditions() => Plugin.ClientState.IsLoggedIn;

    public override void OnOpen() => WindowName = $"{Plugin.L.WhatsNewTitle}##whatsnew";

    public override void PreOpenCheck()
    {
        _version = Plugin.VersionLabel();

        if (_forceOpen)
        {
            IsOpen = true;
            return;
        }

        if (_config.LastSeenVersion == _version) return;

        // Installation fraîche : un nouveau venu a le wizard de configuration,
        // pas l'historique des versions.
        if (Plugin.PluginInterface.Reason == PluginLoadReason.Installer)
        {
            MarkSeen();
            return;
        }

        // Ouverture automatique coupée : on acquitte quand même, sinon rallumer
        // l'option ressortirait les notes d'une version installée de longue date.
        if (!_config.AutoOpenWhatsNew)
        {
            MarkSeen();
            return;
        }

        IsOpen = ReleaseNotes.For(_version) != null;
    }

    public override void Draw()
    {
        var l = Plugin.L;

        // Le bouton reste hors de la zone défilante : c'est la seule sortie de
        // la fenêtre, il ne doit jamais quitter le champ de vision.
        var footer = ImGui.GetFrameHeightWithSpacing() + Theme.S(Theme.GapS * 2f + 6f);

        Text.Title(l.WhatsNewTitle);
        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        Chip.Draw($"v{_version}", ChipTone.Accent, alignToFrame: true);
        Layout.Divider(Theme.GapS);

        using (var body = ImRaii.Child("##whatsnewbody", new Vector2(-1f, -footer)))
        {
            if (body)
            {
                if (ReleaseNotes.All.Length == 0)
                    Feedback.EmptyState(Icons.Sparkle, l.WhatsNewEmpty);
                else
                    DrawHistory(l);
            }
        }

        Layout.Divider(Theme.GapS);

        Layout.Center(Btn.Measure(l.WhatsNewClose, Icons.Check));
        if (!Btn.Draw(l.WhatsNewClose, BtnTone.Primary, BtnSize.Medium, Icons.Check)) return;

        MarkSeen();
        _forceOpen = false;
        IsOpen     = false;
    }

    /// <summary>
    /// Historique complet, plus récent en tête, une section repliable par
    /// version.
    ///
    /// Les versions non encore acquittées sont dépliées, les précédentes
    /// replidées : celui qui a sauté trois versions voit tout ce qu'il a manqué
    /// sans avoir à cliquer, et celui qui vient juste de lire ne se retrouve pas
    /// devant un mur de texte. C'est la logique du widget de changelog
    /// d'OtterGui, que Penumbra et Glamourer utilisent.
    /// </summary>
    private void DrawHistory(Loc l)
    {
        var lastSeen = _config.LastSeenVersion;

        foreach (var note in ReleaseNotes.All)
        {
            var unseen = ReleaseNotes.IsUnseen(note.Version, lastSeen);

            var flags = unseen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            var open  = ImGui.CollapsingHeader($"v{note.Version}  {note.Title}##rel{note.Version}", flags);

            if (unseen)
            {
                // Renvoyée au bout de la ligne plutôt que collée au titre : un
                // titre de version long viendrait sinon buter contre elle.
                ImGui.SameLine(0f, Theme.S(Theme.GapM));
                Layout.RightAlign(Chip.Measure(l.WhatsNewUnseen));
                Chip.Draw(l.WhatsNewUnseen, ChipTone.Accent, alignToFrame: true);
            }

            if (!open) continue;

            MarkdownView.Draw(note.Body);
            Layout.Spacer(Theme.GapM);
        }
    }

    private void MarkSeen()
    {
        // Garde indispensable : PreOpenCheck tourne à chaque frame.
        if (_config.LastSeenVersion == _version) return;

        _config.LastSeenVersion = _version;
        _config.Save();
    }
}
