using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Rédaction d'un texte long de la fiche RP, en jeu.
///
/// Les descriptions se rédigeaient sur le site, faute de place : une zone de
/// saisie de six lignes coincée dans une page qui défile n'est pas un endroit où
/// l'on écrit trois paragraphes. Une fenêtre à part règle cela, et rien d'autre
/// ne l'aurait réglé.
///
/// Le format reste le Markdown du site. Le plugin sait déjà le rendre
/// (<see cref="MarkdownView"/>), l'aperçu montre donc exactement ce que les
/// autres joueurs liront, et le texte reste le même des deux côtés. Un second
/// format aurait demandé un convertisseur et deux rendus qui divergent.
/// </summary>
public sealed class MarkdownEditorWindow : ThemedWindow
{
    private string          _title = string.Empty;
    private string          _text  = string.Empty;
    private int             _max   = 2000;
    private Action<string>? _apply;

    /// <summary>Le texte à l'ouverture, pour savoir s'il a bougé.</summary>
    private string _original = string.Empty;

    /// <summary>Taille d'ouverture, en pixels logiques.</summary>
    private static readonly Vector2 OpenSize = new(900f, 620f);

    /// <summary>La fenêtre doit être posée et dimensionnée à la prochaine image.</summary>
    private bool _placePending;

    /// <summary>Le mémo de syntaxe est déplié. Replié d'une fenêtre à l'autre.</summary>
    private bool _showHelp;

    public MarkdownEditorWindow()
        : base("##mdeditor", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        // Sans contraintes ni taille imposée, la fenêtre se dimensionnait sur
        // son contenu, dont la hauteur se calcule à partir de la place
        // disponible : chaque image changeait la taille de la précédente, et la
        // fenêtre tremblait au lieu de se poser.
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600f, 400f),
            MaximumSize = new Vector2(1600f, 1200f),
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

    /// <summary>
    /// Ouvre l'éditeur sur un champ.
    ///
    /// <paramref name="apply"/> n'est appelé qu'à la validation : fermer la
    /// fenêtre par la croix ou par Échap laisse le champ intact, ce qui est la
    /// seule façon d'abandonner une réécriture sans la regretter.
    /// </summary>
    public void Open(string title, string text, int maxLength, Action<string> apply)
    {
        // Une saisie non validée ne se remplace pas en silence : cliquer
        // « Rédiger » sur une autre section pendant qu'on écrit ferait perdre le
        // paragraphe en cours sans rien dire. La fenêtre reste sur son champ, et
        // le refus se lit dans le pied de page.
        if (IsOpen && _text != _original) return;

        _title    = title;
        _text     = text;
        _original = text;
        _max      = maxLength;
        _apply    = apply;

        _placePending = true;
        IsOpen        = true;
    }

    /// <summary>
    /// Referme l'éditeur sans rien appliquer.
    ///
    /// Appelé quand la page change de personnage ou de fiche : le texte affiché
    /// décrit alors une fiche qui n'est plus à l'écran.
    /// </summary>
    public void Cancel()
    {
        _apply    = null;
        _text     = string.Empty;
        _original = string.Empty;
        IsOpen    = false;
    }

    public override void Draw()
    {
        var l = Plugin.L;

        // Barre d'outils sur la ligne du titre, comme dans un traitement de
        // texte : elle appartient au champ qu'elle met en forme, pas à la
        // fenêtre, et une ligne à part l'en éloignait.
        Layout.SectionHeader(_title, Icons.Edit);
        DrawToolbar(l);

        if (_showHelp)
        {
            Layout.Spacer(Theme.GapXs);
            Text.Small(l.MdEditorHelpBody, Theme.TextMuted);
        }

        Layout.Spacer(Theme.GapS);

        // Saisie sur toute la largeur, aperçu dessous. Côte à côte, chacun
        // n'avait que la moitié de la place, alors que Dalamud ne replie pas les
        // lignes : écrire dans une demi-largeur oblige à revenir à la ligne à la
        // main deux fois plus souvent.
        var avail  = ImGui.GetContentRegionAvail();
        var footer = Theme.S(46f);
        var body   = Math.Max(Theme.S(200f), avail.Y - footer);

        // Deux tiers pour écrire, le reste pour relire : c'est la saisie qu'on
        // regarde, l'aperçu ne sert qu'à vérifier.
        var input   = body * 0.64f;
        var preview = body - input - ImGui.GetTextLineHeightWithSpacing();

        ImGui.InputTextMultiline("##mdinput", ref _text, _max, new Vector2(-1f, input));
        Text.Small($"{_text.Length} / {_max}",
                   _text.Length >= _max ? Theme.Danger : Theme.TextMuted);

        using (var pane = ImRaii.Child("##mdpreview", new Vector2(-1f, preview), true))
        {
            if (pane)
            {
                if (string.IsNullOrWhiteSpace(_text)) Text.Small(l.MdEditorEmpty, Theme.TextMuted);
                else                                  MarkdownView.Draw(_text, Theme.Text);
            }
        }

        Layout.Spacer(Theme.GapXs);

        if (Btn.Draw(l.MdEditorApply, BtnTone.Primary, BtnSize.Medium, Icons.Check, id: "md_apply"))
        {
            _apply?.Invoke(_text);
            IsOpen = false;
        }

        ImGui.SameLine(0f, Theme.S(Theme.GapS));
        if (Btn.Draw(l.MdEditorCancel, BtnTone.Ghost, BtnSize.Medium, id: "md_cancel"))
            IsOpen = false;

        if (_text != _original)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapM));
            Text.Small(l.MdEditorUnsaved, Theme.Gold);
        }
    }

    /// <summary>
    /// Boutons de mise en forme.
    ///
    /// Ils ajoutent leur exemple à la fin du texte et non au curseur : ImGui ne
    /// donne pas la position du curseur d'une zone multiligne sans manipuler son
    /// tampon brut, et une insertion approximative au milieu d'un paragraphe
    /// serait pire que pas d'insertion du tout. On écrit de haut en bas, la fin
    /// du texte est donc l'endroit attendu la plupart du temps.
    /// </summary>
    private void DrawToolbar(Loc l)
    {
        // Des boutons carrés, collés les uns aux autres, comme la barre d'un
        // traitement de texte. Les boutons ordinaires portent une réserve de
        // largeur faite pour un libellé : réduits à une icône, ils s'étalaient
        // sur toute la largeur de la fenêtre.
        //
        // Les exemples viennent de Loc : insérés tels quels dans le texte du
        // joueur, ils doivent être dans sa langue.
        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        Insert(Icons.Bold,       l.MdEditorBold,    $"**{l.MdSampleBold}**", "md_b");
        Tight();
        Insert(Icons.Italic,     l.MdEditorItalic,  $"*{l.MdSampleItalic}*", "md_i");
        Tight();
        Insert(Icons.Heading,    l.MdEditorHeading, $"\n## {l.MdSampleHeading}\n", "md_h");
        Tight();
        Insert(Icons.BulletList, l.MdEditorList,
               $"\n- {l.MdSampleItem1}\n- {l.MdSampleItem2}\n", "md_l");
        Tight();
        Insert(Icons.Quote,      l.MdEditorQuote,   $"\n> {l.MdSampleQuote}\n", "md_q");

        // L'aide au bout de la barre, détachée du groupe et repliée par défaut :
        // l'aperçu enseigne la syntaxe la plupart du temps, la liste ne sert
        // qu'à qui la cherche.
        ImGui.SameLine(0f, Theme.S(Theme.GapM));
        if (Btn.Icon(Icons.Info, "md_help",
                     _showHelp ? BtnTone.Primary : BtnTone.Ghost, l.MdEditorHelp))
            _showHelp = !_showHelp;
    }

    /// <summary>Écart d'une barre d'outils : les boutons se touchent presque.</summary>
    private static void Tight() => ImGui.SameLine(0f, Theme.S(2f));

    private void Insert(FontAwesomeIcon icon, string tooltip, string snippet, string id)
    {
        if (!Btn.Icon(icon, id, BtnTone.Ghost, tooltip)) return;
        if (_text.Length + snippet.Length > _max) return;

        // Un saut de ligne avant un bloc, pour qu'il ne se colle pas au
        // paragraphe précédent et ne soit pas absorbé par lui.
        var needsBreak = snippet.StartsWith('\n') && _text.Length > 0 && !_text.EndsWith('\n');
        _text += needsBreak ? snippet : snippet.TrimStart('\n');
    }
}
