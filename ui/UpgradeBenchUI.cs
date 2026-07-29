using Godot;
using System.Collections.Generic;

/// <summary>
/// Gothic Redesign of the Upgrade Bench UI.
/// Looks like an ancient ritual table with worn parchment cards pinned to engraved stone frames.
/// </summary>
public partial class UpgradeBenchUI : Control {
  private Player currentPlayer;
  private UpgradeBench currentBench;
  private List<UpgradeOption> currentUpgrades;

  // UI elements
  private VBoxContainer cardContainer;
  private HBoxContainer currencyDisplay;
  private Label titleLabel;
  private Button closeButton;

  // Currency icon labels
  private Label echoCount;
  private Label glyphCount;
  private Label essenceCount;

  // Card references
  private List<UpgradeCard> cards = new();

  // Gothic elements
  private Control ritualTable;
  private Control runeGlow;
  private Tween openTween;

  public override void _Ready() {
    AnchorRight = 1.0f;
    AnchorBottom = 1.0f;
    MouseFilter = MouseFilterEnum.Pass;
    Modulate = new Color(1, 1, 1, 0);

    BuildUI();
  }

  public override void _UnhandledInput(InputEvent @event) {
    if (!Visible) return;
    if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.E) {
      OnClosePressed();
    }
  }

  private void BuildUI() {
    // ─── Vignette overlay ──────────────────────────────────────────
    ColorRect vignette = GothicTheme.CreateVignetteOverlay(0.75f);
    vignette.AnchorRight = 1.0f;
    vignette.AnchorBottom = 1.0f;
    vignette.MouseFilter = MouseFilterEnum.Pass;
    AddChild(vignette);

    // ─── Ritual table container ────────────────────────────────────
    ritualTable = new Control();
    ritualTable.AnchorLeft = 0.10f;
    ritualTable.AnchorTop = 0.08f;
    ritualTable.AnchorRight = 0.90f;
    ritualTable.AnchorBottom = 0.92f;
    ritualTable.MouseFilter = MouseFilterEnum.Pass;
    AddChild(ritualTable);

    // ─── Table background (stone slab) ─────────────────────────────
    Panel tablePanel = new Panel();
    tablePanel.AnchorLeft = 0;
    tablePanel.AnchorTop = 0;
    tablePanel.AnchorRight = 1;
    tablePanel.AnchorBottom = 1;
    tablePanel.MouseFilter = MouseFilterEnum.Pass;
    tablePanel.AddThemeStyleboxOverride("panel", GothicTheme.CreatePanelStyle(
      new Color(0.07f, 0.06f, 0.05f, 0.92f),
      GothicTheme.ColorDarkBronze, 4, 10
    ));
    ritualTable.AddChild(tablePanel);

    // ─── Decorative corner ornaments ───────────────────────────────
    AddCornerDecorations(tablePanel);

    // ─── Rune glow effect ──────────────────────────────────────────
    runeGlow = new Control();
    runeGlow.AnchorLeft = 0.30f;
    runeGlow.AnchorTop = 0.01f;
    runeGlow.AnchorRight = 0.70f;
    runeGlow.CustomMinimumSize = new Vector2(0, 50);
    runeGlow.MouseFilter = MouseFilterEnum.Pass;
    ritualTable.AddChild(runeGlow);

    runeGlow.Draw += () => {
      Rect2 rect = runeGlow.GetRect();
      Vector2 center = rect.Size / 2;
      float radius = Mathf.Min(rect.Size.X, rect.Size.Y) * 0.35f;
      runeGlow.DrawArc(center, radius, 0, Mathf.Pi * 2, 32, GothicTheme.ColorDarkBronze, 2.0f);
      runeGlow.DrawArc(center, radius * 0.7f, 0, Mathf.Pi * 2, 24, new Color(0.3f, 0.2f, 0.1f, 0.5f), 1.0f);
      for (int i = 0; i < 8; i++) {
        float angle = (Mathf.Pi * 2 / 8) * i;
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 p1 = center + dir * radius * 0.5f;
        Vector2 p2 = center + dir * radius * 0.85f;
        runeGlow.DrawLine(p1, p2, GothicTheme.ColorAncientGold, 1.5f);
      }
    };

    // ─── Title ─────────────────────────────────────────────────────
    titleLabel = GothicTheme.CreateTitle("ᛉ  RITUAL OF ASCENSION  ᛉ");
    titleLabel.AnchorLeft = 0.20f;
    titleLabel.AnchorTop = 0.08f;
    titleLabel.AnchorRight = 0.80f;
    titleLabel.CustomMinimumSize = new Vector2(0, 32);
    titleLabel.MouseFilter = MouseFilterEnum.Pass;
    ritualTable.AddChild(titleLabel);

    // ─── Currency display row ──────────────────────────────────────
    currencyDisplay = new HBoxContainer();
    currencyDisplay.AnchorLeft = 0.05f;
    currencyDisplay.AnchorTop = 0.14f;
    currencyDisplay.AnchorRight = 0.95f;
    currencyDisplay.CustomMinimumSize = new Vector2(0, 36);
    currencyDisplay.Alignment = BoxContainer.AlignmentMode.Center;
    currencyDisplay.MouseFilter = MouseFilterEnum.Pass;
    ritualTable.AddChild(currencyDisplay);

    echoCount = CreateCurrencyLabel("◆ Echo Fragment", GothicTheme.ColorEchoBlue);
    glyphCount = CreateCurrencyLabel("◈ Ancient Glyph", GothicTheme.ColorGlyphCrimson);
    essenceCount = CreateCurrencyLabel("◇ Forbidden Essence", GothicTheme.ColorBrightGold);

    currencyDisplay.AddChild(echoCount);
    currencyDisplay.AddChild(glyphCount);
    currencyDisplay.AddChild(essenceCount);

    // ─── Card container ────────────────────────────────────────────
    // Use VBoxContainer with SizeFlags (NOT anchors) so cards layout properly
    cardContainer = new VBoxContainer();
    cardContainer.AnchorLeft = 0.05f;
    cardContainer.AnchorTop = 0.20f;
    cardContainer.AnchorRight = 0.95f;
    cardContainer.AnchorBottom = 0.82f;
    cardContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    cardContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
    cardContainer.MouseFilter = MouseFilterEnum.Pass;
    ritualTable.AddChild(cardContainer);

    // ─── Close button ──────────────────────────────────────────────
    closeButton = GothicTheme.CreateGothicButton("✕ CLOSE RITUAL");
    closeButton.AnchorLeft = 0.35f;
    closeButton.AnchorTop = 0.84f;
    closeButton.AnchorRight = 0.65f;
    closeButton.CustomMinimumSize = new Vector2(0, 36);
    closeButton.Pressed += OnClosePressed;
    ritualTable.AddChild(closeButton);
  }

  private void AddCornerDecorations(Panel parent) {
    Control tl = GothicTheme.CreateCornerDecoration();
    tl.AnchorLeft = 0.01f;
    tl.AnchorTop = 0.01f;
    parent.AddChild(tl);
    Control tr = GothicTheme.CreateCornerDecoration();
    tr.AnchorLeft = 1.0f;
    tr.AnchorTop = 0.01f;
    tr.AnchorRight = 0.99f;
    parent.AddChild(tr);
    Control bl = GothicTheme.CreateCornerDecoration();
    bl.AnchorLeft = 0.01f;
    bl.AnchorTop = 1.0f;
    bl.AnchorBottom = 0.99f;
    parent.AddChild(bl);
    Control br = GothicTheme.CreateCornerDecoration();
    br.AnchorLeft = 1.0f;
    br.AnchorTop = 1.0f;
    br.AnchorRight = 0.99f;
    br.AnchorBottom = 0.99f;
    parent.AddChild(br);
  }

  private Label CreateCurrencyLabel(string name, Color color) {
    Label label = new Label();
    label.Text = $"{name}: 0";
    label.Modulate = color;
    label.Theme = GothicTheme.CreateLabelTheme(16, color);
    label.CustomMinimumSize = new Vector2(200, 0);
    label.HorizontalAlignment = HorizontalAlignment.Center;
    label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    return label;
  }

  private Theme CreateLabelTheme(int fontSize) {
    return GothicTheme.CreateLabelTheme(fontSize);
  }

  public void ShowUI(Player player, List<UpgradeOption> upgrades, UpgradeBench bench) {
    currentPlayer = player;
    currentBench = bench;
    currentUpgrades = upgrades;

    RefreshCurrencyDisplay();
    BuildCards();
    Visible = true;

    // Magical reveal animation
    Modulate = new Color(1, 1, 1, 0);
    Scale = new Vector2(0.95f, 0.95f);

    if (openTween != null && openTween.IsValid()) {
      openTween.Kill();
    }
    openTween = CreateTween();
    openTween.SetParallel(true);
    openTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.4f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    openTween.TweenProperty(this, "scale", Vector2.One, 0.4f)
      .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

    if (runeGlow != null) {
      runeGlow.Modulate = new Color(1, 1, 1, 0);
      Tween runeTween = CreateTween();
      runeTween.TweenProperty(runeGlow, "modulate", new Color(1, 1, 1, 1), 0.5f)
        .SetDelay(0.15f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }
  }

  private void RefreshCurrencyDisplay() {
    if (currentPlayer == null) return;
    InventoryComponent inv = currentPlayer.GetComponent<InventoryComponent>();
    if (inv == null) return;

    echoCount.Text = $"◆ Echo Fragment: {inv.AmountOf(ItemType.CURRENCY1)}";
    glyphCount.Text = $"◈ Ancient Glyph: {inv.AmountOf(ItemType.CURRENCY2)}";
    essenceCount.Text = $"◇ Forbidden Essence: {inv.AmountOf(ItemType.CURRENCY3)}";
  }

  private void BuildCards() {
    foreach (UpgradeCard card in cards) {
      card.QueueFree();
    }
    cards.Clear();

    if (currentUpgrades == null) return;

    // Add all 3 cards immediately (no staggering that hides them)
    foreach (UpgradeOption upgrade in currentUpgrades) {
      UpgradeCard card = new UpgradeCard(upgrade, currentPlayer, OnPurchaseClicked);
      cardContainer.AddChild(card);
      cards.Add(card);

      // Subtle fade-in animation
      card.Modulate = new Color(1, 1, 1, 0);
      Tween cardFade = CreateTween();
      cardFade.TweenProperty(card, "modulate", new Color(1, 1, 1, 1), 0.3f)
        .SetDelay(0.2f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }
  }

  private void OnPurchaseClicked(UpgradeOption upgrade) {
    if (currentPlayer == null || currentBench == null) return;

    InventoryComponent inv = currentPlayer.GetComponent<InventoryComponent>();
    if (inv == null) return;

    ItemType currencyType = upgrade.RequiredCurrency switch {
      CurrencyType.EchoFragment => ItemType.CURRENCY1,
      CurrencyType.AncientGlyph => ItemType.CURRENCY2,
      CurrencyType.ForbiddenEssence => ItemType.CURRENCY3,
      _ => ItemType.CURRENCY1
    };

    if (inv.AmountOf(currencyType) < upgrade.CurrencyCost) {
      GD.Print("[UpgradeBench] Not enough currency!");
      return;
    }

    // Find the card that was purchased and show feedback
    UpgradeCard purchasedCard = null;
    foreach (UpgradeCard card in cards) {
      if (card.MatchesUpgrade(upgrade)) {
        purchasedCard = card;
        break;
      }
    }

    // Apply the purchase
    currentBench.OnUpgradePurchased(upgrade, currentPlayer);

    // Show visual feedback on the purchased card
    if (purchasedCard != null) {
      purchasedCard.ShowPurchasedFeedback();
    }

    // Refresh currency display
    RefreshCurrencyDisplay();

    // Disable all cards after purchase
    foreach (UpgradeCard card in cards) {
      card.SetPurchasedState();
    }

    // Close the UI after a short delay so player sees the feedback
    Tween closeDelay = CreateTween();
    closeDelay.TweenCallback(Callable.From(() => {
      OnClosePressed();
    })).SetDelay(1.0f);
  }

  public void OnPurchaseComplete() {
    RefreshCurrencyDisplay();
    foreach (UpgradeCard card in cards) {
      card.UpdateButtonState();
    }
  }

  private void OnClosePressed() {
    if (currentBench != null) {
      currentBench.OnBenchClosed();
    }

    if (openTween != null && openTween.IsValid()) {
      openTween.Kill();
    }
    openTween = CreateTween();
    openTween.SetParallel(true);
    openTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.25f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);
    openTween.TweenProperty(this, "scale", new Vector2(0.95f, 0.95f), 0.25f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);
    openTween.TweenCallback(Callable.From(() => {
      Visible = false;
      Input.MouseMode = Input.MouseModeEnum.Captured;
    })).SetDelay(0.25f);
  }
}

/// <summary>
/// A single upgrade card with gothic ritual parchment styling.
/// </summary>
public partial class UpgradeCard : MarginContainer {
  private UpgradeOption upgrade;
  private Player player;
  private System.Action<UpgradeOption> onPurchase;
  private Button purchaseButton;
  private Panel cardPanel;
  private Label descLabel;
  private Label costLabel;
  private Label nameLabel;
  private Label iconLabel;

  public UpgradeCard(UpgradeOption upgrade, Player player, System.Action<UpgradeOption> onPurchase) {
    this.upgrade = upgrade;
    this.player = player;
    this.onPurchase = onPurchase;
    BuildCard();
  }

  private void BuildCard() {
    SizeFlagsHorizontal = SizeFlags.ExpandFill;
    CustomMinimumSize = new Vector2(0, 80);

    // ─── Stone frame panel ─────────────────────────────────────────
    cardPanel = new Panel();
    cardPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    cardPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
    cardPanel.MouseFilter = MouseFilterEnum.Pass;
    cardPanel.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
      new Color(0.10f, 0.09f, 0.07f, 0.90f),
      GothicTheme.ColorDarkBronze, 2, 6
    ));
    AddChild(cardPanel);

    // ─── Content layout ────────────────────────────────────────────
    HBoxContainer hbox = new HBoxContainer();
    hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
    cardPanel.AddChild(hbox);

    // ─── Upgrade icon ──────────────────────────────────────────────
    iconLabel = new Label();
    iconLabel.Text = GetUpgradeIcon();
    iconLabel.CustomMinimumSize = new Vector2(40, 0);
    iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
    iconLabel.VerticalAlignment = VerticalAlignment.Center;
    iconLabel.Theme = GothicTheme.CreateLabelTheme(24, GothicTheme.ColorAncientGold);
    hbox.AddChild(iconLabel);

    // ─── Description area ──────────────────────────────────────────
    VBoxContainer descBox = new VBoxContainer();
    descBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    descBox.SizeFlagsVertical = SizeFlags.ExpandFill;
    hbox.AddChild(descBox);

    nameLabel = new Label();
    nameLabel.Text = upgrade.Description;
    nameLabel.Theme = GothicTheme.CreateLabelTheme(15, GothicTheme.ColorBrightGold);
    descBox.AddChild(nameLabel);

    string currencyName = upgrade.RequiredCurrency switch {
      CurrencyType.EchoFragment => "Echo Fragment",
      CurrencyType.AncientGlyph => "Ancient Glyph",
      CurrencyType.ForbiddenEssence => "Forbidden Essence",
      _ => "Unknown"
    };

    Color currencyColor = upgrade.RequiredCurrency switch {
      CurrencyType.EchoFragment => GothicTheme.ColorEchoBlue,
      CurrencyType.AncientGlyph => GothicTheme.ColorGlyphCrimson,
      CurrencyType.ForbiddenEssence => GothicTheme.ColorBrightGold,
      _ => Colors.White
    };

    costLabel = new Label();
    costLabel.Text = $"Cost: {upgrade.CurrencyCost}× {currencyName}";
    costLabel.Modulate = currencyColor;
    costLabel.Theme = GothicTheme.CreateLabelTheme(13, currencyColor);
    descBox.AddChild(costLabel);

    // ─── Purchase button ───────────────────────────────────────────
    purchaseButton = GothicTheme.CreateGothicButton("PURCHASE");
    purchaseButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    purchaseButton.SizeFlagsVertical = SizeFlags.ShrinkCenter;
    purchaseButton.CustomMinimumSize = new Vector2(120, 0);
    purchaseButton.Pressed += OnPurchasePressed;
    hbox.AddChild(purchaseButton);

    // ─── Hover effects ─────────────────────────────────────────────
    cardPanel.MouseEntered += OnHoverStart;
    cardPanel.MouseExited += OnHoverEnd;

    UpdateButtonState();
  }

  private string GetUpgradeIcon() {
    string desc = upgrade.Description.ToLower();
    if (desc.Contains("health") || desc.Contains("heal")) return "♥";
    if (desc.Contains("damage") || desc.Contains("attack")) return "⚔";
    if (desc.Contains("speed") || desc.Contains("haste")) return "➤";
    if (desc.Contains("defense") || desc.Contains("armor")) return "🛡";
    if (desc.Contains("magic") || desc.Contains("arcane")) return "✦";
    if (desc.Contains("ammo") || desc.Contains("bullet")) return "●";
    if (desc.Contains("potion") || desc.Contains("flask")) return "⚗";
    return "◇";
  }

  private void OnHoverStart() {
    cardPanel.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
      new Color(0.14f, 0.12f, 0.08f, 0.95f),
      GothicTheme.ColorAncientGold, 3, 6
    ));
    Tween tween = CreateTween();
    tween.SetParallel(true);
    tween.TweenProperty(cardPanel, "scale", new Vector2(1.02f, 1.02f), 0.12f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    tween.TweenProperty(iconLabel, "modulate", GothicTheme.ColorBrightGold, 0.12f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
  }

  private void OnHoverEnd() {
    cardPanel.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
      new Color(0.10f, 0.09f, 0.07f, 0.90f),
      GothicTheme.ColorDarkBronze, 2, 6
    ));
    Tween tween = CreateTween();
    tween.SetParallel(true);
    tween.TweenProperty(cardPanel, "scale", Vector2.One, 0.12f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    tween.TweenProperty(iconLabel, "modulate", GothicTheme.ColorAncientGold, 0.12f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
  }

  private void OnPurchasePressed() {
    onPurchase?.Invoke(upgrade);
  }

  public void UpdateButtonState() {
    if (player == null) return;
    InventoryComponent inv = player.GetComponent<InventoryComponent>();
    if (inv == null) return;

    ItemType currencyType = upgrade.RequiredCurrency switch {
      CurrencyType.EchoFragment => ItemType.CURRENCY1,
      CurrencyType.AncientGlyph => ItemType.CURRENCY2,
      CurrencyType.ForbiddenEssence => ItemType.CURRENCY3,
      _ => ItemType.CURRENCY1
    };

    bool canAfford = inv.AmountOf(currencyType) >= upgrade.CurrencyCost;
    purchaseButton.Disabled = !canAfford;

    if (!canAfford) {
      purchaseButton.Modulate = new Color(0.5f, 0.3f, 0.3f, 0.7f);
    } else {
      purchaseButton.Modulate = new Color(1, 1, 1, 1);
    }
  }

  /// <summary>
  /// Checks if this card matches the given upgrade option.
  /// </summary>
  public bool MatchesUpgrade(UpgradeOption other) {
    return upgrade.Description == other.Description &&
           upgrade.EffectType == other.EffectType &&
           upgrade.EffectValue == other.EffectValue;
  }

  /// <summary>
  /// Shows a brief flash animation on the purchased card.
  /// </summary>
  public void ShowPurchasedFeedback() {
    // Flash the card gold
    Tween flashTween = CreateTween();
    flashTween.TweenProperty(cardPanel, "modulate", new Color(1.5f, 1.3f, 0.8f, 1.0f), 0.15f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    flashTween.TweenProperty(cardPanel, "modulate", new Color(0.4f, 0.4f, 0.4f, 0.8f), 0.4f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);

    // Change button text to "PURCHASED"
    purchaseButton.Text = "✓ PURCHASED";
    purchaseButton.Disabled = true;

    // Gray out the card
    nameLabel.Modulate = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    costLabel.Modulate = new Color(0.4f, 0.4f, 0.4f, 1.0f);
    iconLabel.Modulate = new Color(0.4f, 0.4f, 0.4f, 1.0f);
  }

  /// <summary>
  /// Disables the card after a purchase has been made (grayed out).
  /// </summary>
  public void SetPurchasedState() {
    purchaseButton.Disabled = true;
    purchaseButton.Modulate = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    // Gray out the card content
    nameLabel.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    costLabel.Modulate = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    iconLabel.Modulate = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    cardPanel.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.7f);
  }

  private static Theme CreateLabelTheme(int fontSize) {
    return GothicTheme.CreateLabelTheme(fontSize);
  }
}
