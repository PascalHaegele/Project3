using Godot;
using System.Collections.Generic;

/// <summary>
/// Prototype UI for the Upgrade Bench.
/// Shows 3 random upgrades, currency amounts, and purchase buttons.
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

  public override void _Ready() {
    AnchorRight = 1.0f;
    AnchorBottom = 1.0f;
    MouseFilter = MouseFilterEnum.Ignore;

    BuildUI();
  }

  public override void _UnhandledInput(InputEvent @event) {
    if (!Visible) return;
    if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.E) {
      OnClosePressed();
    }
  }

  private void BuildUI() {
    // Semi-transparent background
    ColorRect bg = new ColorRect();
    bg.Color = new Color(0.0f, 0.0f, 0.0f, 0.7f);
    bg.AnchorRight = 1.0f;
    bg.AnchorBottom = 1.0f;
    bg.MouseFilter = MouseFilterEnum.Pass;
    AddChild(bg);

    // Center container
    VBoxContainer mainContainer = new VBoxContainer();
    mainContainer.AnchorRight = 1.0f;
    mainContainer.AnchorBottom = 1.0f;
    mainContainer.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    mainContainer.SizeFlagsVertical = SizeFlags.ShrinkCenter;
    AddChild(mainContainer);

    // Title
    titleLabel = new Label();
    titleLabel.Text = "UPGRADE BENCH";
    titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
    titleLabel.Theme = CreateLabelTheme(28);
    titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    mainContainer.AddChild(titleLabel);

    // Currency display row
    currencyDisplay = new HBoxContainer();
    currencyDisplay.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    currencyDisplay.Alignment = BoxContainer.AlignmentMode.Center;
    mainContainer.AddChild(currencyDisplay);

    echoCount = CreateCurrencyLabel("Echo Fragment", new Color(0.6f, 0.8f, 1.0f));
    glyphCount = CreateCurrencyLabel("Ancient Glyph", new Color(1.0f, 0.8f, 0.4f));
    essenceCount = CreateCurrencyLabel("Forbidden Essence", new Color(1.0f, 0.3f, 0.3f));

    currencyDisplay.AddChild(echoCount);
    currencyDisplay.AddChild(glyphCount);
    currencyDisplay.AddChild(essenceCount);

    // Spacer
    Control spacer = new Control();
    spacer.CustomMinimumSize = new Vector2(0, 20);
    mainContainer.AddChild(spacer);

    // Card container
    cardContainer = new VBoxContainer();
    cardContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    cardContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
    mainContainer.AddChild(cardContainer);

    // Close button
    closeButton = new Button();
    closeButton.Text = "CLOSE";
    closeButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    closeButton.Pressed += OnClosePressed;
    mainContainer.AddChild(closeButton);
  }

  private Label CreateCurrencyLabel(string name, Color color) {
    Label label = new Label();
    label.Text = $"{name}: 0";
    label.Modulate = color;
    label.Theme = CreateLabelTheme(16);
    label.CustomMinimumSize = new Vector2(200, 0);
    label.HorizontalAlignment = HorizontalAlignment.Center;
    return label;
  }

  private Theme CreateLabelTheme(int fontSize) {
    Theme t = new Theme();
    LabelSettings ls = new LabelSettings();
    ls.FontSize = fontSize;
    ls.OutlineSize = 2;
    ls.OutlineColor = Colors.Black;
    t.Set("Label/label_settings", ls);
    return t;
  }

  /// <summary>
  /// Shows the upgrade bench UI with generated upgrades.
  /// </summary>
  public void ShowUI(Player player, List<UpgradeOption> upgrades, UpgradeBench bench) {
    currentPlayer = player;
    currentBench = bench;
    currentUpgrades = upgrades;

    RefreshCurrencyDisplay();
    BuildCards();
    Visible = true;
  }

  private void RefreshCurrencyDisplay() {
    if (currentPlayer == null) return;
    InventoryComponent inv = currentPlayer.GetComponent<InventoryComponent>();
    if (inv == null) return;

    echoCount.Text = $"Echo Fragment: {inv.AmountOf(ItemType.CURRENCY1)}";
    glyphCount.Text = $"Ancient Glyph: {inv.AmountOf(ItemType.CURRENCY2)}";
    essenceCount.Text = $"Forbidden Essence: {inv.AmountOf(ItemType.CURRENCY3)}";
  }

  private void BuildCards() {
    // Clear existing cards
    foreach (UpgradeCard card in cards) {
      card.QueueFree();
    }
    cards.Clear();

    if (currentUpgrades == null) return;

    foreach (UpgradeOption upgrade in currentUpgrades) {
      UpgradeCard card = new UpgradeCard(upgrade, currentPlayer, OnPurchaseClicked);
      cardContainer.AddChild(card);
      cards.Add(card);
    }
  }

  private void OnPurchaseClicked(UpgradeOption upgrade) {
    if (currentPlayer == null || currentBench == null) return;

    // Check if player has enough currency
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

    currentBench.OnUpgradePurchased(upgrade, currentPlayer);
  }

  private void OnClosePressed() {
    if (currentBench != null) {
      currentBench.OnBenchClosed();
    }
    Visible = false;
    Input.MouseMode = Input.MouseModeEnum.Captured;
  }
}

/// <summary>
/// A single upgrade card showing description, required currency, and purchase button.
/// </summary>
public partial class UpgradeCard : MarginContainer {
  private UpgradeOption upgrade;
  private Player player;
  private System.Action<UpgradeOption> onPurchase;
  private Button purchaseButton;

  public UpgradeCard(UpgradeOption upgrade, Player player, System.Action<UpgradeOption> onPurchase) {
    this.upgrade = upgrade;
    this.player = player;
    this.onPurchase = onPurchase;
    BuildCard();
  }

  private void BuildCard() {
    SizeFlagsHorizontal = SizeFlags.ExpandFill;
    CustomMinimumSize = new Vector2(0, 80);

    // Card background
    Panel panel = new Panel();
    panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    panel.SizeFlagsVertical = SizeFlags.ExpandFill;
    AddChild(panel);

    HBoxContainer hbox = new HBoxContainer();
    hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
    panel.AddChild(hbox);

    // Left side: description
    VBoxContainer descBox = new VBoxContainer();
    descBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    descBox.SizeFlagsVertical = SizeFlags.ExpandFill;
    hbox.AddChild(descBox);

    Label descLabel = new Label();
    descLabel.Text = upgrade.Description;
    descLabel.Theme = CreateLabelTheme(18);
    descLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    descBox.AddChild(descLabel);

    // Currency requirement
    string currencyName = upgrade.RequiredCurrency switch {
      CurrencyType.EchoFragment => "Echo Fragment",
      CurrencyType.AncientGlyph => "Ancient Glyph",
      CurrencyType.ForbiddenEssence => "Forbidden Essence",
      _ => "Unknown"
    };

    Color currencyColor = upgrade.RequiredCurrency switch {
      CurrencyType.EchoFragment => new Color(0.6f, 0.8f, 1.0f),
      CurrencyType.AncientGlyph => new Color(1.0f, 0.8f, 0.4f),
      CurrencyType.ForbiddenEssence => new Color(1.0f, 0.3f, 0.3f),
      _ => Colors.White
    };

    Label costLabel = new Label();
    costLabel.Text = $"Cost: {upgrade.CurrencyCost}x {currencyName}";
    costLabel.Modulate = currencyColor;
    costLabel.Theme = CreateLabelTheme(14);
    descBox.AddChild(costLabel);

    // Right side: purchase button
    purchaseButton = new Button();
    purchaseButton.Text = "PURCHASE";
    purchaseButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    purchaseButton.SizeFlagsVertical = SizeFlags.ShrinkCenter;
    purchaseButton.CustomMinimumSize = new Vector2(120, 0);
    purchaseButton.Pressed += OnPurchasePressed;
    hbox.AddChild(purchaseButton);

    // Update button state
    UpdateButtonState();
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

    purchaseButton.Disabled = inv.AmountOf(currencyType) < upgrade.CurrencyCost;
  }

  private static Theme CreateLabelTheme(int fontSize) {
    Theme t = new Theme();
    LabelSettings ls = new LabelSettings();
    ls.FontSize = fontSize;
    ls.OutlineSize = 2;
    ls.OutlineColor = Colors.Black;
    t.Set("Label/label_settings", ls);
    return t;
  }
}