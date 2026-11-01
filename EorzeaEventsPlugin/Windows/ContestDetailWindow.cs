using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Détail d'un concours, sur le modèle de la lecture d'une actualité :
/// description, lots, conditions, résultat du tirage et état du compte.
///
/// Rien n'est demandé au serveur : la fenêtre lit à chaque image la dernière
/// version du concours dans la liste de la page Concours, et ne garde l'objet
/// reçu à l'ouverture qu'à défaut. Ouverte pendant qu'on s'inscrit sur le site,
/// elle passe ainsi d'elle-même à « Tu participes ».
/// </summary>
public class ContestDetailWindow : ThemedWindow
{
    private readonly Func<string, ContestItemDto?> _latest;
    private ContestItemDto? _item;

    /// <param name="latest">Version la plus récente d'un concours par son id, null si absent.</param>
    public ContestDetailWindow(Func<string, ContestItemDto?> latest) : base("##contestdetail", ImGuiWindowFlags.None)
    {
        _latest = latest;
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 360),
            MaximumSize = new Vector2(820, 900),
        };
    }

    public void Open(ContestItemDto item)
    {
        _item      = item;
        WindowName = Glyphs.Safe(item.Title) + "##contestdetail";
        IsOpen     = true;
    }

    protected override void DrawContents()
    {
        if (_item is not { } snapshot) return;
        var c = _latest(snapshot.Id) ?? snapshot;
        var l = Plugin.L;

        // Le titre suit la langue ; seul ce qui suit « ## » identifie la fenêtre.
        var name = Glyphs.Safe(c.Title) + "##contestdetail";
        if (WindowName != name) WindowName = name;

        Cover.Draw(Textures.Get(NewsView.AbsoluteUrl(c.ImageUrl)));

        ContestView.Chips(c, l);
        Layout.Spacer(Theme.GapXs);
        Text.TitleWrapped(c.Title, Theme.Accent);
        ContestView.Dates(c, l);

        Layout.Divider(Theme.GapS);
        MarkdownView.Draw(c.Description, Theme.Text);

        if (c.Prizes.Count > 0)
        {
            Layout.Spacer(Theme.GapM);
            ContestView.Prizes(c, l);
        }

        if (c.Conditions.Count > 0)
        {
            Layout.Spacer(Theme.GapM);
            Layout.SectionHeader(l.ContestConditions, Icons.Check);
            foreach (var cond in c.Conditions)
            {
                var label = cond.Required ? cond.Label : $"{cond.Label} ({l.ContestOptional})";
                Text.WithIcon(Icons.Check, label, Theme.TextFaint, Theme.Text, wrap: true);
            }
        }

        if (c.Phase == "drawn" && c.Winners.Count > 0)
        {
            Layout.Spacer(Theme.GapM);
            Layout.SectionHeader(l.ContestWinners, Icons.Trophy);
            foreach (var w in c.Winners)
            {
                var label = w.Prize is { } prize ? $"{w.Name}  —  {prize}" : w.Name;
                Text.WithIcon(Icons.Trophy, label, Theme.Gold, Theme.Text, wrap: true);
            }
        }

        if (c.Me is { Entered: true } me)
        {
            Layout.Spacer(Theme.GapM);
            Layout.SectionHeader(l.ContestYourEntry, Icons.Profile);
            // Lot supprimé après le tirage : « Tu as gagné » seul, plutôt qu'un lot vide.
            if (me.Won)                    Text.Wrapped(me.Prize is { } prize ? string.Format(l.ContestYouWon, prize) : l.ContestWon, Theme.Gold);
            else if (c.Phase == "drawn")   Text.Wrapped(l.ContestNotDrawn, Theme.TextMuted);
            else                           Text.Wrapped(l.ContestYouEntered, Theme.Text);
        }

        Layout.Divider(Theme.GapM);
        ContestView.SiteButton(c, l, "contestdetail_site");
    }
}
