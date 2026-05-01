using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

internal static class UiStyle
{
    // ── Text hierarchy ────────────────────────────────────────────────────────
    public static readonly Vector4 TextTitle   = new(0.88f, 0.80f, 0.60f, 1.00f);
    public static readonly Vector4 TextSection = new(0.76f, 0.70f, 0.54f, 1.00f);
    public static readonly Vector4 TextMuted   = new(1.00f, 1.00f, 1.00f, 0.72f);
    public static readonly Vector4 TextSubtle  = new(1.00f, 1.00f, 1.00f, 0.50f);

    // ── Status colors (en-têtes de groupes) ───────────────────────────────────
    public static readonly Vector4 StatusOpen  = new(0.30f, 0.90f, 0.50f, 1.00f);
    public static readonly Vector4 StatusSoon  = new(0.50f, 0.80f, 1.00f, 1.00f);
    public static readonly Vector4 StatusLater = new(0.90f, 0.70f, 0.30f, 1.00f);

    // ── Card ──────────────────────────────────────────────────────────────────
    public static readonly Vector4 CardBg     = new(0.12f, 0.12f, 0.14f, 1.00f);
    public static readonly Vector4 CardBorder = new(0.25f, 0.25f, 0.30f, 0.70f);

    // ── Chips ─────────────────────────────────────────────────────────────────
    public static readonly Vector4 ChipBg      = new(0.24f, 0.24f, 0.28f, 1.00f);
    public static readonly Vector4 ChipBgOpen  = new(0.15f, 0.38f, 0.20f, 1.00f);
    public static readonly Vector4 ChipBgSoon  = new(0.18f, 0.28f, 0.42f, 1.00f);
    public static readonly Vector4 ChipBgLater = new(0.35f, 0.28f, 0.12f, 1.00f);
    public static readonly Vector4 ChipBgAccent = new(0.23f, 0.31f, 0.45f, 1.00f);

    // ── Buttons — Primary (bleu-indigo) ───────────────────────────────────────
    public static readonly Vector4 PrimaryNormal  = new(0.34f, 0.36f, 0.88f, 1.00f);
    public static readonly Vector4 PrimaryHovered = new(0.44f, 0.46f, 0.95f, 1.00f);
    public static readonly Vector4 PrimaryActive  = new(0.25f, 0.27f, 0.78f, 1.00f);

    // ── Buttons — Success (vert) ──────────────────────────────────────────────
    public static readonly Vector4 SuccessNormal  = new(0.15f, 0.62f, 0.28f, 1.00f);
    public static readonly Vector4 SuccessHovered = new(0.20f, 0.72f, 0.35f, 1.00f);
    public static readonly Vector4 SuccessActive  = new(0.10f, 0.52f, 0.22f, 1.00f);

    // ── Buttons — Danger (rouge) ──────────────────────────────────────────────
    public static readonly Vector4 DangerNormal  = new(0.80f, 0.15f, 0.15f, 1.00f);
    public static readonly Vector4 DangerHovered = new(0.90f, 0.20f, 0.20f, 1.00f);
    public static readonly Vector4 DangerActive  = new(0.70f, 0.10f, 0.10f, 1.00f);

    // ── Buttons — Secondary (neutre) ──────────────────────────────────────────
    public static readonly Vector4 SecondaryNormal  = new(0.24f, 0.24f, 0.28f, 1.00f);
    public static readonly Vector4 SecondaryHovered = new(0.28f, 0.28f, 0.33f, 1.00f);
    public static readonly Vector4 SecondaryActive  = new(0.20f, 0.20f, 0.24f, 1.00f);

    // ── Tailles boutons ───────────────────────────────────────────────────────
    public static readonly Vector2 SmallButton   = new( 92f, 0f);
    public static readonly Vector2 MediumButton  = new(120f, 0f);
    public static readonly Vector2 WideButton    = new(160f, 0f);
    public static readonly Vector2 PrimaryButton = new(180f, 0f);

    // ── Espacements & arrondis ────────────────────────────────────────────────
    public const float CardRounding  = 6f;
    public const float CardPadH      = 12f;
    public const float CardPadV      = 7f;
    public const float CardSpacing   = 6f;
    public const float ChipPadH      = 6f;
    public const float ChipPadV      = 2f;
    public const float ChipRounding     = 4f;
    public const float InlineSpacing    = 6f;
    public const float EstabBannerHeight = 90f;
}
