using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeaEventsPlugin.Api;
using EorzeaEventsPlugin.Ui;
using EorzeaEventsPlugin.Ui.Components;
using EorzeaEventsPlugin.Ui.Shell;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Lecture d'une actualité, sur le modèle de la fiche d'événement.
///
/// Dépliée dans la liste, une actualité longue s'étirait dans une carte étroite,
/// ses intertitres soulignés comme des séparateurs de cartes, et repoussait les
/// suivantes hors de vue. La liste garde l'accroche ; le texte entier se lit ici.
///
/// Rien n'est demandé au serveur : la liste porte déjà le texte complet.
/// </summary>
public class NewsDetailWindow : ThemedWindow
{
    private NewsItemDto? _item;

    public NewsDetailWindow() : base("##newsdetail", ImGuiWindowFlags.None)
    {
        LogicalSizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 360),
            MaximumSize = new Vector2(820, 900),
        };
    }

    public void Open(NewsItemDto item)
    {
        _item      = item;
        WindowName = Glyphs.Safe(item.Title) + "##newsdetail";
        IsOpen     = true;
    }

    protected override void DrawContents()
    {
        if (_item is not { } item) return;
        var l = Plugin.L;

        Cover.Draw(Textures.Get(NewsView.AbsoluteUrl(item.ImageUrl)));

        NewsView.Header(item, l);
        Layout.Spacer(Theme.GapXs);

        // H1 et non H2 : les intertitres du texte sont en H2, et le titre ne
        // s'en distinguait plus que par la couleur.
        Text.TitleWrapped(item.Title, Theme.Accent);

        Layout.Divider(Theme.GapS);
        NewsView.Body(item.Content);

        Layout.Divider(Theme.GapM);
        if (Btn.Draw(l.ViewOnline, BtnTone.Secondary, BtnSize.Medium, Icons.External, id: "newsdetail_site"))
            NewsView.OpenOnSite(item.Path);
    }
}
