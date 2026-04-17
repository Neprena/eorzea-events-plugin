using System.Numerics;

namespace EorzeaEventsPlugin.Windows;

internal static class UiSizes
{
    public static readonly Vector2 SmallButton   = new(92f,  0f);
    public static readonly Vector2 MediumButton  = new(120f, 0f);
    public static readonly Vector2 WideButton    = new(160f, 0f);
    public static readonly Vector2 PrimaryButton = new(180f, 0f);
}

internal static class UiColors
{
    // Bleu-indigo (action principale)
    public static readonly Vector4 PrimaryNormal  = new(0.34f, 0.36f, 0.88f, 1f);
    public static readonly Vector4 PrimaryHovered = new(0.44f, 0.46f, 0.95f, 1f);
    public static readonly Vector4 PrimaryActive  = new(0.25f, 0.27f, 0.78f, 1f);

    // Vert (action positive)
    public static readonly Vector4 SuccessNormal  = new(0.15f, 0.62f, 0.28f, 1f);
    public static readonly Vector4 SuccessHovered = new(0.20f, 0.72f, 0.35f, 1f);
    public static readonly Vector4 SuccessActive  = new(0.10f, 0.52f, 0.22f, 1f);

    // Rouge (action destructrice)
    public static readonly Vector4 DangerNormal  = new(0.80f, 0.15f, 0.15f, 1f);
    public static readonly Vector4 DangerHovered = new(0.90f, 0.20f, 0.20f, 1f);
    public static readonly Vector4 DangerActive  = new(0.70f, 0.10f, 0.10f, 1f);
}
