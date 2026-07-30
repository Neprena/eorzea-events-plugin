using EorzeaEventsPlugin.Ui;
using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

/// <summary>
/// Façade de transition vers <see cref="Theme"/>.
///
/// Tous les membres redirigent vers les jetons de design centralisés, ce qui
/// reteinte l'ensemble du plugin sans toucher aux sites d'appel existants.
/// Ce fichier disparaît une fois toutes les fenêtres migrées vers
/// <c>Ui.Theme</c> et <c>Ui.Components</c>.
///
/// Les métriques sont des propriétés et non des champs : l'échelle de
/// l'interface Dalamud peut changer à chaud, une constante figée au chargement
/// rendrait l'interface illisible à 150 %.
/// </summary>
internal static class UiStyle
{
    // ── Hiérarchie de texte ───────────────────────────────────────────────────
    public static readonly Vector4 TextTitle   = Theme.Gold;
    public static readonly Vector4 TextSection = Theme.GoldHover;
    public static readonly Vector4 TextMuted   = Theme.TextMuted;
    public static readonly Vector4 TextSubtle  = Theme.TextFaint;

    // ── Couleurs de statut (en-têtes de groupes) ─────────────────────────────
    public static readonly Vector4 StatusOpen  = Theme.Online;
    public static readonly Vector4 StatusSoon  = Theme.Accent;
    public static readonly Vector4 StatusLater = Theme.Idle;

    // ── Card ──────────────────────────────────────────────────────────────────
    public static readonly Vector4 CardBg     = Theme.BgSurface;
    public static readonly Vector4 CardBorder = Theme.Border;

    // ── Chips ─────────────────────────────────────────────────────────────────
    public static readonly Vector4 ChipBg       = Theme.BgRaised;
    public static readonly Vector4 ChipBgOpen   = Theme.Mix(Theme.BgRaised, Theme.Online, 0.30f);
    public static readonly Vector4 ChipBgSoon   = Theme.Mix(Theme.BgRaised, Theme.Link,   0.30f);
    public static readonly Vector4 ChipBgLater  = Theme.Mix(Theme.BgRaised, Theme.Idle,   0.30f);
    public static readonly Vector4 ChipBgAccent = Theme.Mix(Theme.BgRaised, Theme.Accent, 0.35f);

    // ── Boutons ───────────────────────────────────────────────────────────────
    public static readonly Vector4 PrimaryNormal  = Theme.Accent;
    public static readonly Vector4 PrimaryHovered = Theme.Mix(Theme.Accent, Theme.Text, 0.18f);
    public static readonly Vector4 PrimaryActive  = Theme.AccentActive;

    public static readonly Vector4 SuccessNormal  = Theme.Online;
    public static readonly Vector4 SuccessHovered = Theme.Mix(Theme.Online, Theme.Text,   0.18f);
    public static readonly Vector4 SuccessActive  = Theme.Mix(Theme.Online, Theme.BgDeep, 0.25f);

    public static readonly Vector4 DangerNormal  = Theme.Danger;
    public static readonly Vector4 DangerHovered = Theme.Mix(Theme.Danger, Theme.Text, 0.18f);
    public static readonly Vector4 DangerActive  = Theme.DangerHover;

    public static readonly Vector4 SecondaryNormal  = Theme.BgRaised;
    public static readonly Vector4 SecondaryHovered = Theme.BgHover;
    public static readonly Vector4 SecondaryActive  = Theme.BgSurface;

    // ── Tailles de boutons ────────────────────────────────────────────────────
    public static Vector2 SmallButton   => new(Theme.S( 92f), 0f);
    public static Vector2 MediumButton  => new(Theme.S(120f), 0f);
    public static Vector2 WideButton    => new(Theme.S(160f), 0f);
    public static Vector2 PrimaryButton => new(Theme.S(180f), 0f);

    // ── Espacements et arrondis ───────────────────────────────────────────────
    public static float CardRounding  => Theme.S(Theme.RadiusCard);
    public static float CardPadH      => Theme.S(Theme.CardPadX);
    public static float CardPadV      => Theme.S(Theme.CardPadY);
    public static float CardSpacing   => Theme.S(Theme.GapM);
    public static float ChipPadH      => Theme.S(Theme.GapM);
    public static float ChipPadV      => Theme.S(3f);
    public static float ChipRounding  => Theme.S(Theme.RadiusPill);
    public static float InlineSpacing => Theme.S(Theme.GapS);

    public static float EstabBannerHeight => Theme.S(96f);
}
