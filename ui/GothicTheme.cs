using Godot;
using System;

/// <summary>
/// Shared helper class for creating gothic-styled UI elements.
/// Provides consistent textures, colors, and styling across Inventory and Upgrade Bench UIs.
/// </summary>
public static class GothicTheme {
  // ─── Color Palette ──────────────────────────────────────────────
  public static readonly Color ColorBlack = new Color(0.05f, 0.05f, 0.06f, 1.0f);
  public static readonly Color ColorDarkCharcoal = new Color(0.10f, 0.10f, 0.12f, 1.0f);
  public static readonly Color ColorGunmetal = new Color(0.15f, 0.15f, 0.18f, 1.0f);
  public static readonly Color ColorDarkBronze = new Color(0.35f, 0.22f, 0.10f, 1.0f);
  public static readonly Color ColorAncientGold = new Color(0.70f, 0.55f, 0.20f, 1.0f);
  public static readonly Color ColorBrightGold = new Color(0.90f, 0.75f, 0.30f, 1.0f);
  public static readonly Color ColorPagesPurple = new Color(0.55f, 0.30f, 0.65f, 1.0f);
  public static readonly Color ColorEchoBlue = new Color(0.30f, 0.50f, 0.80f, 1.0f);
  public static readonly Color ColorGlyphCrimson = new Color(0.70f, 0.15f, 0.15f, 1.0f);
  public static readonly Color ColorHealthRed = new Color(0.55f, 0.08f, 0.08f, 1.0f);
  public static readonly Color ColorInsanityYellow = new Color(0.75f, 0.65f, 0.15f, 1.0f);
  public static readonly Color ColorParchment = new Color(0.20f, 0.17f, 0.13f, 1.0f);
  public static readonly Color ColorParchmentLight = new Color(0.28f, 0.24f, 0.18f, 1.0f);
  public static readonly Color ColorStone = new Color(0.12f, 0.12f, 0.14f, 1.0f);
  public static readonly Color ColorStoneBorder = new Color(0.20f, 0.18f, 0.16f, 1.0f);
  
  // ─── Font Sizes ─────────────────────────────────────────────────
  public const int FontSizeTitle = 28;
  public const int FontSizeSubtitle = 20;
  public const int FontSizeBody = 16;
  public const int FontSizeSmall = 13;
  public const int FontSizeTiny = 11;

  // ─── Animation Duration ─────────────────────────────────────────
  public const float AnimOpenDuration = 0.35f;
  public const float AnimCloseDuration = 0.25f;
  public const float AnimHoverDuration = 0.15f;

  // ─── StyleBox Creation ──────────────────────────────────────────

  /// <summary>
  /// Creates a layered panel background with an engraved stone border.
  /// </summary>
  public static StyleBoxFlat CreatePanelStyle(Color innerColor, Color borderColor, int borderWidth = 2, int cornerRadius = 4) {
    return new StyleBoxFlat {
      BgColor = innerColor,
      BorderWidthLeft = borderWidth,
      BorderWidthTop = borderWidth,
      BorderWidthRight = borderWidth,
      BorderWidthBottom = borderWidth,
      BorderColor = borderColor,
      CornerRadiusTopLeft = cornerRadius,
      CornerRadiusTopRight = cornerRadius,
      CornerRadiusBottomRight = cornerRadius,
      CornerRadiusBottomLeft = cornerRadius,
      ContentMarginLeft = 8,
      ContentMarginTop = 6,
      ContentMarginRight = 8,
      ContentMarginBottom = 6,
      ExpandMarginLeft = 1,
      ExpandMarginTop = 1,
      ExpandMarginRight = 1,
      ExpandMarginBottom = 1,
      ShadowSize = 4,
      ShadowColor = new Color(0.0f, 0.0f, 0.0f, 0.4f),
      ShadowOffset = new Vector2(2, 2),
    };
  }

  /// <summary>
  /// Creates a gothic engraved frame style (for socket slots, item frames).
  /// </summary>
  public static StyleBoxFlat CreateFrameStyle(Color innerColor, Color borderColor, int borderWidth = 3, int cornerRadius = 6) {
    return new StyleBoxFlat {
      BgColor = innerColor,
      BorderWidthLeft = borderWidth,
      BorderWidthTop = borderWidth,
      BorderWidthRight = borderWidth,
      BorderWidthBottom = borderWidth,
      BorderColor = borderColor,
      CornerRadiusTopLeft = cornerRadius,
      CornerRadiusTopRight = cornerRadius,
      CornerRadiusBottomRight = cornerRadius,
      CornerRadiusBottomLeft = cornerRadius,
      ContentMarginLeft = 12,
      ContentMarginTop = 8,
      ContentMarginRight = 12,
      ContentMarginBottom = 8,
    };
  }

  /// <summary>
  /// Creates a thin divider line with gothic styling.
  /// </summary>
  public static StyleBoxFlat CreateDividerStyle(Color color, float height = 2) {
    return new StyleBoxFlat {
      BgColor = color,
      ContentMarginLeft = 0,
      ContentMarginTop = 0,
      ContentMarginRight = 0,
      ContentMarginBottom = (int)height,
    };
  }

  // ─── Label Creation ─────────────────────────────────────────────

  public static Label CreateTitle(string text) {
    return new Label {
      Text = text,
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center,
      AutowrapMode = TextServer.AutowrapMode.Off,
      Theme = CreateLabelTheme(FontSizeTitle, new Color(0.80f, 0.65f, 0.30f, 1.0f)),
    };
  }

  public static Label CreateSubtitle(string text) {
    return new Label {
      Text = text,
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Center,
      Theme = CreateLabelTheme(FontSizeSubtitle, new Color(0.65f, 0.50f, 0.20f, 1.0f)),
    };
  }

  public static Label CreateBody(string text, Color? color = null) {
    return new Label {
      Text = text,
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Top,
      AutowrapMode = TextServer.AutowrapMode.Word,
      Theme = CreateLabelTheme(FontSizeBody, color ?? new Color(0.75f, 0.70f, 0.60f, 1.0f)),
    };
  }

  public static Label CreateSmall(string text, Color? color = null) {
    return new Label {
      Text = text,
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Center,
      Theme = CreateLabelTheme(FontSizeSmall, color ?? new Color(0.55f, 0.50f, 0.40f, 1.0f)),
    };
  }

  // ─── Theme / Label Settings ─────────────────────────────────────

  public static Theme CreateLabelTheme(int fontSize, Color? fontColor = null, Color? outlineColor = null) {
    Theme t = new Theme();
    LabelSettings ls = new LabelSettings {
      FontSize = fontSize,
      OutlineSize = 1,
      OutlineColor = outlineColor ?? new Color(0.0f, 0.0f, 0.0f, 0.6f),
      FontColor = fontColor ?? new Color(0.80f, 0.75f, 0.65f, 1.0f),
    };
    t.Set("Label/label_settings", ls);
    return t;
  }

  // ─── Background Creation ────────────────────────────────────────

  /// <summary>
  /// Creates a full-screen dark overlay with a subtle vignette effect.
  /// </summary>
  public static ColorRect CreateVignetteOverlay(float alpha = 0.75f) {
    return new ColorRect {
      Color = new Color(0.0f, 0.0f, 0.0f, alpha),
      MouseFilter = Control.MouseFilterEnum.Pass,
    };
  }

  // ─── Decorative Elements ────────────────────────────────────────

  /// <summary>
  /// Creates a decorative gothic separator bar (horizontal line with ornaments).
  /// </summary>
  public static Control CreateGothicSeparator(float width = 0, Color? color = null) {
    Color c = color ?? ColorDarkBronze;
    var sep = new Control();
    sep.CustomMinimumSize = new Vector2(width > 0 ? width : 20, 20);
    sep.Draw += () => {
      Rect2 rect = sep.GetRect();
      float midY = rect.Size.Y / 2;
      sep.DrawRect(new Rect2(0, midY - 1, rect.Size.X, 2), c);
      sep.DrawCircle(new Vector2(6, midY), 3, ColorAncientGold);
      sep.DrawCircle(new Vector2(rect.Size.X - 6, midY), 3, ColorAncientGold);
      Vector2 center = new Vector2(rect.Size.X / 2, midY);
      sep.DrawCircle(center, 4, ColorBrightGold);
    };
    return sep;
  }

  /// <summary>
  /// Creates an engraved frame corner decoration.
  /// </summary>
  public static Control CreateCornerDecoration() {
    var deco = new Control();
    deco.CustomMinimumSize = new Vector2(16, 16);
    deco.Draw += () => {
      Rect2 rect = deco.GetRect();
      float size = Mathf.Min(rect.Size.X, rect.Size.Y);
      Vector2 topLeft = new Vector2(2, 2);
      Vector2 topRight = new Vector2(rect.Size.X - 2, 2);
      Vector2 bottomLeft = new Vector2(2, rect.Size.Y - 2);
      Vector2 bottomRight = new Vector2(rect.Size.X - 2, rect.Size.Y - 2);
      
      deco.DrawArc(topLeft, size * 0.3f, Mathf.Pi, Mathf.Pi * 1.5f, 16, ColorAncientGold, 1.5f);
      deco.DrawArc(topRight, size * 0.3f, Mathf.Pi * 1.5f, Mathf.Pi * 2.0f, 16, ColorAncientGold, 1.5f);
      deco.DrawArc(bottomLeft, size * 0.3f, Mathf.Pi * 0.5f, Mathf.Pi, 16, ColorAncientGold, 1.5f);
      deco.DrawArc(bottomRight, size * 0.3f, 0, Mathf.Pi * 0.5f, 16, ColorAncientGold, 1.5f);
    };
    return deco;
  }

  // ─── Tween Helpers ──────────────────────────────────────────────

  /// <summary>
  /// Fades in a control with a subtle scale-up and alpha transition.
  /// </summary>
  public static void FadeInControl(Control control, Node parent, float duration = 0.3f) {
    control.Modulate = new Color(1, 1, 1, 0);
    control.Scale = new Vector2(0.95f, 0.95f);
    
    Tween tween = parent.CreateTween();
    tween.SetParallel(true);
    tween.TweenProperty(control, "modulate", new Color(1, 1, 1, 1), duration).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    tween.TweenProperty(control, "scale", Vector2.One, duration).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
  }

  /// <summary>
  /// Fades out a control with scale-down and alpha transition.
  /// </summary>
  public static void FadeOutControl(Control control, Node parent, float duration = 0.25f) {
    Tween tween = parent.CreateTween();
    tween.SetParallel(true);
    tween.TweenProperty(control, "modulate", new Color(1, 1, 1, 0), duration).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);
    tween.TweenProperty(control, "scale", new Vector2(0.95f, 0.95f), duration).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);
  }

  /// <summary>
  /// Creates a gothic-styled button with stone/engraved appearance.
  /// </summary>
  public static Button CreateGothicButton(string text) {
    var btn = new Button();
    btn.Text = text;
    btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
    btn.CustomMinimumSize = new Vector2(160, 36);
    
    // Normal state - dark stone with bronze border
    var normalStyle = new StyleBoxFlat {
      BgColor = new Color(0.08f, 0.08f, 0.10f, 0.9f),
      BorderWidthLeft = 2,
      BorderWidthTop = 2,
      BorderWidthRight = 2,
      BorderWidthBottom = 2,
      BorderColor = ColorDarkBronze,
      CornerRadiusTopLeft = 3,
      CornerRadiusTopRight = 3,
      CornerRadiusBottomRight = 3,
      CornerRadiusBottomLeft = 3,
      ContentMarginLeft = 12,
      ContentMarginTop = 4,
      ContentMarginRight = 12,
      ContentMarginBottom = 4,
      ShadowSize = 3,
      ShadowColor = new Color(0, 0, 0, 0.5f),
      ShadowOffset = new Vector2(1, 1),
    };
    
    // Hover state - golden glow
    var hoverStyle = new StyleBoxFlat {
      BgColor = new Color(0.15f, 0.12f, 0.08f, 0.9f),
      BorderWidthLeft = 2,
      BorderWidthTop = 2,
      BorderWidthRight = 2,
      BorderWidthBottom = 2,
      BorderColor = ColorAncientGold,
      CornerRadiusTopLeft = 3,
      CornerRadiusTopRight = 3,
      CornerRadiusBottomRight = 3,
      CornerRadiusBottomLeft = 3,
      ContentMarginLeft = 12,
      ContentMarginTop = 4,
      ContentMarginRight = 12,
      ContentMarginBottom = 4,
      ShadowSize = 4,
      ShadowColor = new Color(0.5f, 0.4f, 0.1f, 0.3f),
      ShadowOffset = new Vector2(1, 1),
    };
    
    // Pressed state
    var pressedStyle = new StyleBoxFlat {
      BgColor = new Color(0.05f, 0.05f, 0.06f, 0.95f),
      BorderWidthLeft = 2,
      BorderWidthTop = 2,
      BorderWidthRight = 2,
      BorderWidthBottom = 2,
      BorderColor = ColorBrightGold,
      CornerRadiusTopLeft = 3,
      CornerRadiusTopRight = 3,
      CornerRadiusBottomRight = 3,
      CornerRadiusBottomLeft = 3,
      ContentMarginLeft = 12,
      ContentMarginTop = 4,
      ContentMarginRight = 12,
      ContentMarginBottom = 4,
    };
    
    btn.AddThemeStyleboxOverride("normal", normalStyle);
    btn.AddThemeStyleboxOverride("hover", hoverStyle);
    btn.AddThemeStyleboxOverride("pressed", pressedStyle);
    btn.AddThemeStyleboxOverride("focus", hoverStyle);
    
    var fontTheme = new Theme();
    var ls = new LabelSettings {
      FontSize = FontSizeBody,
      FontColor = new Color(0.75f, 0.65f, 0.40f, 1.0f),
      OutlineSize = 1,
      OutlineColor = new Color(0.0f, 0.0f, 0.0f, 0.5f),
    };
    fontTheme.Set("Button/label_settings", ls);
    btn.Theme = fontTheme;
    
    return btn;
  }
}