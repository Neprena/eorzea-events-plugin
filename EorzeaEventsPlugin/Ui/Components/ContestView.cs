using Dalamud.Bindings.ImGui;
using EorzeaEventsPlugin.Api;
using System.Numerics;

namespace EorzeaEventsPlugin.Ui.Components;

/// <summary>
/// Morceaux communs à la carte d'un concours et à sa fenêtre de détail :
/// pastilles, ligne de dates, lot principal, bouton vers le site.
///
/// La phase vient du serveur (upcoming, open, closed, drawn) : l'horloge du
/// joueur n'entre pas en jeu.
/// </summary>
internal static class ContestView
{
    /// <summary>
    /// Le joueur vient d'ouvrir le site pour s'inscrire. La page Concours
    /// s'y abonne pour relire la liste plus souvent un moment : sans ça, la
    /// carte garderait son invitation à participer jusqu'au relevé suivant.
    /// </summary>
    public static event Action? EnterClicked;

    /// <summary>Inscriptions ouvertes et pas encore inscrit : le bouton invite à participer.</summary>
    public static bool CanEnter(ContestItemDto c) => c.Phase == "open" && c.Me?.Entered != true;

    /// <summary>Phase, mise en avant et état du compte, sur une ligne.</summary>
    public static void Chips(ContestItemDto c, Loc l)
    {
        var (label, tone, icon) = c.Phase switch
        {
            "open"     => (l.ContestPhaseOpen,     ChipTone.Success, Icons.Clock),
            "upcoming" => (l.ContestPhaseUpcoming, ChipTone.Accent,  Icons.Clock),
            "drawn"    => (l.ContestPhaseDrawn,    ChipTone.Neutral, Icons.Trophy),
            _          => (l.ContestPhaseClosed,   ChipTone.Warning, Icons.Clock),
        };
        Chip.Draw(label, tone, icon);

        if (c.IsFeatured)
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(l.ContestFeatured, ChipTone.Gold, Icons.Sparkle);
        }

        if (c.Me is { Won: true })
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(l.ContestWon, ChipTone.Gold, Icons.Trophy);
        }
        else if (c.Me is { Entered: true })
        {
            ImGui.SameLine(0f, Theme.S(Theme.GapXs));
            Chip.Draw(l.ContestEntered, ChipTone.Success, Icons.Check);
        }
    }

    /// <summary>
    /// La date qui compte selon la phase : fin des inscriptions, ouverture,
    /// tirage prévu, ou fin du concours.
    /// </summary>
    public static void Dates(ContestItemDto c, Loc l)
    {
        var text = c.Phase switch
        {
            "open"     => string.Format(l.ContestUntil,   Date(c.EndAt, l, withTime: true)),
            "upcoming" => string.Format(l.ContestOpensOn, Date(c.StartAt, l, withTime: true)),
            "closed" when c.DrawAt is { } draw => string.Format(l.ContestDrawOn, Date(draw, l, withTime: true)),
            _          => string.Format(l.ContestEndedOn, Date(c.EndAt, l, withTime: false)),
        };

        using var _ = Fonts.PushSmall();
        Text.WithIcon(Icons.Clock, text, Theme.TextFaint, Theme.TextMuted);
    }

    /// <summary>
    /// Tous les lots, dans l'ordre des rangs, avec leur quantité s'il y en a
    /// plusieurs. Rien sans lot. N'en montrer que le premier laissait croire
    /// qu'il n'y avait que celui-là à gagner. Sous un titre de section, comme
    /// les autres blocs du plugin ; l'icône de chaque ligne reste discrète.
    /// </summary>
    public static void Prizes(ContestItemDto c, Loc l)
    {
        if (c.Prizes.Count == 0) return;

        Layout.SectionHeader(l.ContestPrizes, Icons.Gift);
        foreach (var p in c.Prizes)
        {
            var label = p.Quantity > 1 ? $"{p.Label}  ×{p.Quantity}" : p.Label;
            Text.WithIcon(Icons.Gift, label, Theme.TextFaint, Theme.Text, wrap: true);
        }
    }

    /// <summary>
    /// Vers la page du concours sur le site. L'or est réservé à l'invitation à
    /// participer ; une fois inscrit, ou hors inscriptions, le bouton est secondaire.
    /// </summary>
    public static void SiteButton(ContestItemDto c, Loc l, string id)
    {
        var enter = CanEnter(c);
        if (Btn.Draw(enter ? l.ContestEnterOnSite : l.ViewOnline,
                     enter ? BtnTone.Primary : BtnTone.Secondary,
                     BtnSize.Medium, Icons.External, id: id))
        {
            NewsView.OpenOnSite(c.Path);
            if (enter) EnterClicked?.Invoke();
        }
    }

    /// <summary>
    /// Jour et mois à la façon de chaque langue (« 16 novembre », « 16. November »,
    /// « November 16 »), l'année seulement si ce n'est pas l'année en cours.
    ///
    /// L'heure compte pour une échéance : un concours qui ferme le 16 à 00:00
    /// affiché « jusqu'au 16 novembre » laissait croire que le 16 comptait encore.
    /// </summary>
    public static string Date(DateTime at, Loc l, bool withTime)
    {
        var local = at.ToLocalTime();
        var text  = local.ToString("M", l.Culture);
        if (local.Year != DateTime.Now.Year) text += " " + local.Year.ToString(l.Culture);
        return withTime ? $"{text}, {local.ToString("t", l.Culture)}" : text;
    }
}
