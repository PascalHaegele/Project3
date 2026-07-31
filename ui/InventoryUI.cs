using Godot;
using FmodSharp;
using System.Collections.Generic;

/// <summary>
/// Full-screen Inventory UI - Gothic Redesign.
/// Looks like an ancient cursed book with engraved stone panels, parchment pages, and golden accents.
/// </summary>
public partial class InventoryUI : Control {
  private InventoryComponent inventory;
  private SocketComponent socketComponent;
  private Weapon weapon;

  private VBoxContainer weaponSocketContainer;
  private VBoxContainer armorSocketContainer;
  private VBoxContainer skillSocketContainer;
  private VBoxContainer pagesListContainer;
  private PanelContainer tooltipPanel;
  private Label ammoLabel;
  private Label potionLabel;
  private Label weaponNameLabel;
  private Button closeButton;

  private Label tooltipName;
  private Label tooltipDescription;
  private Label tooltipModifier;
  private Label tooltipRarity;

  private List<SocketSlot> weaponSlots = new();
  private List<SocketSlot> armorSlots = new();
  private List<SocketSlot> skillSlots = new();
  private PageData selectedPage;
  private PageData selectedPageForSocket;

  // Gothic UI elements
  private Control mainContainer;
  private Control bookLeftPage;
  private Control bookRightPage;
  private Control statsPanel;
  private Tween openTween;

  [Signal] public delegate void InventoryClosedEventHandler();

  public override void _Ready() {
    Visible = false;
    Modulate = new Color(1, 1, 1, 0);
    MouseFilter = MouseFilterEnum.Pass;

    BuildUI();
    CreateSocketSlots();

    if (closeButton != null) {
      closeButton.Pressed += OnClosePressed;
    }
  }

  private void BuildUI() {
    weaponSocketContainer = GetNodeOrNull<VBoxContainer>("WeaponSocketContainer");
    armorSocketContainer = GetNodeOrNull<VBoxContainer>("ArmorSocketContainer");
    skillSocketContainer = GetNodeOrNull<VBoxContainer>("SkillSocketContainer");
    pagesListContainer = GetNodeOrNull<VBoxContainer>("PagesList");
    closeButton = GetNodeOrNull<Button>("CloseButton");

    if (weaponSocketContainer != null) return;

    AnchorRight = 1.0f;
    AnchorBottom = 1.0f;

    // ─── Vignette overlay ──────────────────────────────────────────
    ColorRect vignette = GothicTheme.CreateVignetteOverlay(0.70f);
    vignette.MouseFilter = MouseFilterEnum.Pass;
    AddChild(vignette);

    // ─── Main book container ───────────────────────────────────────
    mainContainer = new Control();
    mainContainer.AnchorRight = 1.0f;
    mainContainer.AnchorBottom = 1.0f;
    mainContainer.MouseFilter = MouseFilterEnum.Pass;
    AddChild(mainContainer);

    // ─── Book background panel (the "book" itself) ─────────────────
    Panel bookPanel = new Panel();
    bookPanel.AnchorLeft = 0.05f;
    bookPanel.AnchorTop = 0.05f;
    bookPanel.AnchorRight = 0.95f;
    bookPanel.AnchorBottom = 0.95f;
    bookPanel.MouseFilter = MouseFilterEnum.Pass;
    bookPanel.AddThemeStyleboxOverride("panel", GothicTheme.CreatePanelStyle(
      new Color(0.08f, 0.07f, 0.06f, 0.92f),
      GothicTheme.ColorDarkBronze, 3, 8
    ));
    mainContainer.AddChild(bookPanel);

    // ─── Decorative corner ornaments ───────────────────────────────
    AddCornerDecorations(bookPanel);

    // ─── Title bar ─────────────────────────────────────────────────
    Label title = GothicTheme.CreateTitle("ᛉ  ARCANE CODEX  ᛉ");
    title.AnchorLeft = 0.3f;
    title.AnchorTop = 0.02f;
    title.AnchorRight = 0.7f;
    title.CustomMinimumSize = new Vector2(0, 40);
    title.MouseFilter = MouseFilterEnum.Pass;
    mainContainer.AddChild(title);

    // ─── Gothic separator under title ──────────────────────────────
    Control titleSep = GothicTheme.CreateGothicSeparator();
    titleSep.AnchorLeft = 0.2f;
    titleSep.AnchorTop = 0.07f;
    titleSep.AnchorRight = 0.8f;
    titleSep.CustomMinimumSize = new Vector2(0, 20);
    titleSep.MouseFilter = MouseFilterEnum.Pass;
    mainContainer.AddChild(titleSep);

    // ─── LEFT PAGE: Sockets ────────────────────────────────────────
    bookLeftPage = new Control();
    bookLeftPage.AnchorLeft = 0.08f;
    bookLeftPage.AnchorTop = 0.10f;
    bookLeftPage.AnchorRight = 0.48f;
    bookLeftPage.AnchorBottom = 0.90f;
    bookLeftPage.MouseFilter = MouseFilterEnum.Pass;
    mainContainer.AddChild(bookLeftPage);

    // Left page inner panel (parchment-like)
    Panel leftPagePanel = new Panel();
    leftPagePanel.AnchorLeft = 0;
    leftPagePanel.AnchorTop = 0;
    leftPagePanel.AnchorRight = 1;
    leftPagePanel.AnchorBottom = 1;
    leftPagePanel.MouseFilter = MouseFilterEnum.Pass;
    leftPagePanel.AddThemeStyleboxOverride("panel", GothicTheme.CreatePanelStyle(
      GothicTheme.ColorParchment,
      GothicTheme.ColorStoneBorder, 2, 4
    ));
    bookLeftPage.AddChild(leftPagePanel);

    // Left page content
    VBoxContainer leftContent = new VBoxContainer();
    leftContent.AnchorLeft = 0.02f;
    leftContent.AnchorTop = 0.02f;
    leftContent.AnchorRight = 0.98f;
    leftContent.AnchorBottom = 0.98f;
    leftContent.MouseFilter = MouseFilterEnum.Pass;
    bookLeftPage.AddChild(leftContent);

    // ─── SOCKETS SECTION ───────────────────────────────────────────
    Label socketsTitle = GothicTheme.CreateSubtitle("◈  SOCKETS  ◈");
    leftContent.AddChild(socketsTitle);

    // Weapon Sockets
    Label weaponLabel = GothicTheme.CreateSmall("⚔ WEAPON SOCKETS", GothicTheme.ColorAncientGold);
    leftContent.AddChild(weaponLabel);

    weaponSocketContainer = new VBoxContainer();
    weaponSocketContainer.Name = "WeaponSocketContainer";
    weaponSocketContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    leftContent.AddChild(weaponSocketContainer);

    // Armor Sockets
    Label armorLabel = GothicTheme.CreateSmall("🛡 ARMOR SOCKETS", GothicTheme.ColorAncientGold);
    leftContent.AddChild(armorLabel);

    armorSocketContainer = new VBoxContainer();
    armorSocketContainer.Name = "ArmorSocketContainer";
    armorSocketContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    leftContent.AddChild(armorSocketContainer);

    // Skill Sockets
    Label skillLabel = GothicTheme.CreateSmall("✦ SKILL SOCKETS", GothicTheme.ColorAncientGold);
    leftContent.AddChild(skillLabel);

    skillSocketContainer = new VBoxContainer();
    skillSocketContainer.Name = "SkillSocketContainer";
    skillSocketContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    leftContent.AddChild(skillSocketContainer);

    // ─── RIGHT PAGE: Pages & Tooltip ───────────────────────────────
    bookRightPage = new Control();
    bookRightPage.AnchorLeft = 0.52f;
    bookRightPage.AnchorTop = 0.10f;
    bookRightPage.AnchorRight = 0.92f;
    bookRightPage.AnchorBottom = 0.90f;
    bookRightPage.MouseFilter = MouseFilterEnum.Pass;
    mainContainer.AddChild(bookRightPage);

    // Right page inner panel (parchment-like)
    Panel rightPagePanel = new Panel();
    rightPagePanel.AnchorLeft = 0;
    rightPagePanel.AnchorTop = 0;
    rightPagePanel.AnchorRight = 1;
    rightPagePanel.AnchorBottom = 1;
    rightPagePanel.MouseFilter = MouseFilterEnum.Pass;
    rightPagePanel.AddThemeStyleboxOverride("panel", GothicTheme.CreatePanelStyle(
      GothicTheme.ColorParchment,
      GothicTheme.ColorStoneBorder, 2, 4
    ));
    bookRightPage.AddChild(rightPagePanel);

    // Right page content
    VBoxContainer rightContent = new VBoxContainer();
    rightContent.AnchorLeft = 0.02f;
    rightContent.AnchorTop = 0.02f;
    rightContent.AnchorRight = 0.98f;
    rightContent.AnchorBottom = 0.98f;
    rightContent.MouseFilter = MouseFilterEnum.Pass;
    bookRightPage.AddChild(rightContent);

    Label pagesTitle = GothicTheme.CreateSubtitle("📜 COLLECTED PAGES");
    rightContent.AddChild(pagesTitle);

    // Scrollable pages list
    ScrollContainer scroll = new ScrollContainer();
    scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
    scroll.CustomMinimumSize = new Vector2(0, 200);
    rightContent.AddChild(scroll);

    pagesListContainer = new VBoxContainer();
    pagesListContainer.Name = "PagesList";
    pagesListContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    scroll.AddChild(pagesListContainer);

    // ─── TOOLTIP (ancient scroll) ──────────────────────────────────
    tooltipPanel = new PanelContainer();
    tooltipPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    tooltipPanel.CustomMinimumSize = new Vector2(0, 140);
    tooltipPanel.Visible = false;
    tooltipPanel.MouseFilter = MouseFilterEnum.Pass;
    tooltipPanel.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
      new Color(0.12f, 0.10f, 0.08f, 0.95f),
      GothicTheme.ColorDarkBronze, 2, 4
    ));
    rightContent.AddChild(tooltipPanel);

    VBoxContainer tooltipVBox = new VBoxContainer();
    tooltipPanel.AddChild(tooltipVBox);

    tooltipName = new Label();
    tooltipName.Theme = GothicTheme.CreateLabelTheme(20, GothicTheme.ColorBrightGold);
    tooltipVBox.AddChild(tooltipName);

    tooltipRarity = new Label();
    tooltipRarity.Theme = GothicTheme.CreateLabelTheme(14, GothicTheme.ColorPagesPurple);
    tooltipVBox.AddChild(tooltipRarity);

    Control tooltipSep = new Control();
    tooltipSep.CustomMinimumSize = new Vector2(0, 4);
    tooltipVBox.AddChild(tooltipSep);

    tooltipDescription = new Label();
    tooltipDescription.Theme = GothicTheme.CreateLabelTheme(14, new Color(0.70f, 0.65f, 0.55f, 1.0f));
    tooltipDescription.AutowrapMode = TextServer.AutowrapMode.Word;
    tooltipVBox.AddChild(tooltipDescription);

    tooltipModifier = new Label();
    tooltipModifier.Theme = GothicTheme.CreateLabelTheme(13, new Color(0.60f, 0.55f, 0.40f, 1.0f));
    tooltipVBox.AddChild(tooltipModifier);

    // ─── STATS PANEL (bottom bar) ──────────────────────────────────
    statsPanel = new Control();
    statsPanel.AnchorLeft = 0.08f;
    statsPanel.AnchorTop = 0.92f;
    statsPanel.AnchorRight = 0.92f;
    statsPanel.CustomMinimumSize = new Vector2(0, 50);
    statsPanel.MouseFilter = MouseFilterEnum.Pass;
    mainContainer.AddChild(statsPanel);

    Panel statsBg = new Panel();
    statsBg.AnchorLeft = 0;
    statsBg.AnchorTop = 0;
    statsBg.AnchorRight = 1;
    statsBg.AnchorBottom = 1;
    statsBg.MouseFilter = MouseFilterEnum.Pass;
    statsBg.AddThemeStyleboxOverride("panel", GothicTheme.CreatePanelStyle(
      new Color(0.06f, 0.05f, 0.04f, 0.90f),
      GothicTheme.ColorDarkBronze, 2, 4
    ));
    statsPanel.AddChild(statsBg);

    HBoxContainer statsHBox = new HBoxContainer();
    statsHBox.AnchorLeft = 0.02f;
    statsHBox.AnchorTop = 0.05f;
    statsHBox.AnchorRight = 0.98f;
    statsHBox.AnchorBottom = 0.95f;
    statsHBox.MouseFilter = MouseFilterEnum.Pass;
    statsPanel.AddChild(statsHBox);

    weaponNameLabel = GothicTheme.CreateBody("Weapon: None", GothicTheme.ColorAncientGold);
    weaponNameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    statsHBox.AddChild(weaponNameLabel);

    ammoLabel = GothicTheme.CreateBody("Ammo: 0/0", GothicTheme.ColorEchoBlue);
    ammoLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    statsHBox.AddChild(ammoLabel);

    potionLabel = GothicTheme.CreateBody("Potions: 0", GothicTheme.ColorHealthRed);
    potionLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    statsHBox.AddChild(potionLabel);

    // Close button
    closeButton = GothicTheme.CreateGothicButton("✕ CLOSE CODEX");
    closeButton.Name = "CloseButton";
    closeButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    statsHBox.AddChild(closeButton);
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

  private Theme CreateLabelTheme(int fontSize) {
    return GothicTheme.CreateLabelTheme(fontSize);
  }

  public void Initialize(InventoryComponent inv, SocketComponent socket, Weapon w) {
    inventory = inv;
    socketComponent = socket;
    weapon = w;
    if (inventory != null) {
      inventory.InventoryChanged += RefreshUI;
    }
  }

  public void Open() {
    if (inventory == null) return;
    Visible = true;
    MouseFilter = MouseFilterEnum.Pass;
    Input.MouseMode = Input.MouseModeEnum.Visible;
    RefreshUI();

    // Book opening animation
    Modulate = new Color(1, 1, 1, 0);
    Scale = new Vector2(0.92f, 0.92f);

    if (openTween != null && openTween.IsValid()) {
      openTween.Kill();
    }
    openTween = CreateTween();
    openTween.SetParallel(true);
    openTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.35f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    openTween.TweenProperty(this, "scale", Vector2.One, 0.35f)
      .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

    if (bookLeftPage != null) {
      bookLeftPage.Modulate = new Color(1, 1, 1, 0);
      Tween leftTween = CreateTween();
      leftTween.TweenProperty(bookLeftPage, "modulate", new Color(1, 1, 1, 1), 0.3f)
        .SetDelay(0.1f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }
    if (bookRightPage != null) {
      bookRightPage.Modulate = new Color(1, 1, 1, 0);
      Tween rightTween = CreateTween();
      rightTween.TweenProperty(bookRightPage, "modulate", new Color(1, 1, 1, 1), 0.3f)
        .SetDelay(0.15f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }
    if (statsPanel != null) {
      statsPanel.Modulate = new Color(1, 1, 1, 0);
      Tween statsTween = CreateTween();
      statsTween.TweenProperty(statsPanel, "modulate", new Color(1, 1, 1, 1), 0.25f)
        .SetDelay(0.2f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }
  }

  public void Close() {
    if (openTween != null && openTween.IsValid()) {
      openTween.Kill();
    }
    openTween = CreateTween();
    openTween.SetParallel(true);
    openTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.25f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);
    openTween.TweenProperty(this, "scale", new Vector2(0.92f, 0.92f), 0.25f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);
    openTween.TweenCallback(Callable.From(() => {
      Visible = false;
      Input.MouseMode = Input.MouseModeEnum.Captured;
      _ = EmitSignal(SignalName.InventoryClosed);
    })).SetDelay(0.25f);
  }

  public void Toggle() {
    if (Visible) Close();
    else Open();
  }

  public bool IsOpen => Visible;

  private void RefreshUI() {
    RefreshPagesList();
    RefreshSocketSlots();
    RefreshStats();
    RefreshTooltip();
  }

  private void RefreshPagesList() {
    if (pagesListContainer == null) return;
    foreach (Node child in pagesListContainer.GetChildren()) {
      child.QueueFree();
    }
    if (inventory == null) return;

    bool hasAny = false;
    foreach (PageData page in inventory.collectedPages) {
      hasAny = true;

      PageData captured = page;
      bool isSelected = (selectedPageForSocket == page);

      PanelContainer pageEntry = new PanelContainer();
      pageEntry.SizeFlagsHorizontal = SizeFlags.ExpandFill;
      pageEntry.CustomMinimumSize = new Vector2(0, 50);
      pageEntry.MouseFilter = MouseFilterEnum.Stop;
      pageEntry.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
        isSelected ? new Color(0.20f, 0.17f, 0.10f, 0.95f) : new Color(0.15f, 0.13f, 0.10f, 0.85f),
        isSelected ? GothicTheme.ColorBrightGold : GothicTheme.ColorStoneBorder,
        isSelected ? 3 : 1, 3
      ));

      pageEntry.MouseEntered += () => {
        if (selectedPageForSocket != captured) {
          pageEntry.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
            new Color(0.20f, 0.17f, 0.12f, 0.90f),
            GothicTheme.ColorAncientGold, 2, 3
          ));
        }
        Tween hoverTween = CreateTween();
        hoverTween.TweenProperty(pageEntry, "scale", new Vector2(1.02f, 1.02f), 0.1f)
          .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        selectedPage = captured;
        RefreshTooltip();
      };
      pageEntry.MouseExited += () => {
        if (selectedPageForSocket != captured) {
          pageEntry.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
            new Color(0.15f, 0.13f, 0.10f, 0.85f),
            GothicTheme.ColorStoneBorder, 1, 3
          ));
        } else {
          pageEntry.AddThemeStyleboxOverride("panel", GothicTheme.CreateFrameStyle(
            new Color(0.20f, 0.17f, 0.10f, 0.95f),
            GothicTheme.ColorBrightGold, 3, 3
          ));
        }
        Tween hoverTween = CreateTween();
        hoverTween.TweenProperty(pageEntry, "scale", Vector2.One, 0.1f)
          .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
      };

      HBoxContainer pageRow = new HBoxContainer();
      pageRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
      pageRow.MouseFilter = MouseFilterEnum.Pass;
      pageEntry.AddChild(pageRow);

      Label iconLabel = new Label();
      iconLabel.Text = "📜";
      iconLabel.CustomMinimumSize = new Vector2(30, 0);
      iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
      iconLabel.Theme = GothicTheme.CreateLabelTheme(18);
      iconLabel.MouseFilter = MouseFilterEnum.Pass;
      pageRow.AddChild(iconLabel);

      VBoxContainer pageInfo = new VBoxContainer();
      pageInfo.SizeFlagsHorizontal = SizeFlags.ExpandFill;
      pageInfo.MouseFilter = MouseFilterEnum.Pass;
      pageRow.AddChild(pageInfo);

      Color rarityColor = page.Rarity switch {
        "Common" => new Color(0.60f, 0.60f, 0.60f, 1.0f),
        "Uncommon" => GothicTheme.ColorEchoBlue,
        "Rare" => GothicTheme.ColorPagesPurple,
        "Epic" => GothicTheme.ColorBrightGold,
        "Legendary" => new Color(1.0f, 0.3f, 0.1f, 1.0f),
        _ => new Color(0.60f, 0.60f, 0.60f, 1.0f),
      };

      Label nameLabel = new Label();
      nameLabel.Text = page.PageName;
      nameLabel.Theme = GothicTheme.CreateLabelTheme(15, rarityColor);
      nameLabel.MouseFilter = MouseFilterEnum.Pass;
      pageInfo.AddChild(nameLabel);

      Label effectsLabel = new Label();
      effectsLabel.Text = $"⚔ {PageData.GetEffectDescription(page.WeaponEffect)}\n" +
                        $"🛡 {PageData.GetEffectDescription(page.ArmorEffect)}\n" +
                        $"✦ {PageData.GetEffectDescription(page.SkillEffect)}";
      effectsLabel.Theme = GothicTheme.CreateLabelTheme(11, new Color(0.50f, 0.45f, 0.35f, 1.0f));
      effectsLabel.MouseFilter = MouseFilterEnum.Pass;
      effectsLabel.AutowrapMode = TextServer.AutowrapMode.Word;
      pageInfo.AddChild(effectsLabel);

      if (selectedPageForSocket == page) {
        Label selLabel = new Label();
        selLabel.Text = "◈";
        selLabel.Theme = GothicTheme.CreateLabelTheme(18, GothicTheme.ColorBrightGold);
        selLabel.CustomMinimumSize = new Vector2(24, 0);
        selLabel.HorizontalAlignment = HorizontalAlignment.Center;
        selLabel.MouseFilter = MouseFilterEnum.Pass;
        pageRow.AddChild(selLabel);
      }

      pageEntry.GuiInput += (InputEvent @event) => {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
          OnPageClicked(captured);
        }
      };

      pagesListContainer.AddChild(pageEntry);
    }

    if (!hasAny) {
      Label emptyLabel = new Label();
      emptyLabel.Text = "No pages collected...\nThe Codex awaits knowledge.";
      emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
      emptyLabel.Theme = GothicTheme.CreateLabelTheme(14, new Color(0.40f, 0.35f, 0.25f, 1.0f));
      emptyLabel.CustomMinimumSize = new Vector2(0, 60);
      pagesListContainer.AddChild(emptyLabel);
    }
  }

  private void CreateSocketSlots() {
    if (weaponSocketContainer == null || armorSocketContainer == null || skillSocketContainer == null) return;

    for (int i = 0; i < 4; i++) {
      SocketSlot slot = new SocketSlot("Weapon", i);
      slot.SlotClicked += OnSlotClicked;
      weaponSlots.Add(slot);
      weaponSocketContainer.AddChild(slot);
    }
    for (int i = 0; i < 3; i++) {
      SocketSlot slot = new SocketSlot("Armor", i);
      slot.SlotClicked += OnSlotClicked;
      armorSlots.Add(slot);
      armorSocketContainer.AddChild(slot);
    }
    for (int i = 0; i < 3; i++) {
      SocketSlot slot = new SocketSlot("Skill", i);
      slot.SlotClicked += OnSlotClicked;
      skillSlots.Add(slot);
      skillSocketContainer.AddChild(slot);
    }
  }

  private void RefreshSocketSlots() {
    foreach (SocketSlot slot in weaponSlots) slot.ClearPage();
    foreach (SocketSlot slot in armorSlots) slot.ClearPage();
    foreach (SocketSlot slot in skillSlots) slot.ClearPage();

    if (socketComponent == null) return;

    Dictionary<string, PageData> socketed = socketComponent.GetAllSocketedPages();
    foreach (var kvp in socketed) {
      PageData page = kvp.Value;
      string slotId = kvp.Key;
      string category = slotId.Split('_')[0];
      string[] parts = slotId.Split('_');
      int slotIndex = parts.Length > 1 ? int.Parse(parts[1]) : 0;

      SocketSlot slot = GetSlotByCategoryIndex(category, slotIndex);
      if (slot != null) slot.SetPage(page);
    }
  }

  private void RefreshStats() {
    if (inventory == null) return;
    if (potionLabel != null) potionLabel.Text = $"Potions: {inventory.items[(int)ItemType.POTION]}";
    if (weapon != null && weaponNameLabel != null) weaponNameLabel.Text = $"Weapon: {weapon.Name}";
    if (weapon != null && ammoLabel != null) ammoLabel.Text = $"Ammo: {weapon.CurrentAmmo} / {weapon.info.magazineSize}";
  }

  private void RefreshTooltip() {
    if (tooltipPanel == null) return;
    if (selectedPage != null) {
      tooltipName.Text = selectedPage.PageName;
      tooltipRarity.Text = $"[ {selectedPage.Rarity} ]";
      tooltipDescription.Text = selectedPage.Description;
      tooltipModifier.Text = $"⚔ Weapon: {PageData.GetEffectDisplay(selectedPage.WeaponEffect, selectedPage.WeaponEffectValue)}\n" +
                            $"🛡 Armor:  {PageData.GetEffectDisplay(selectedPage.ArmorEffect, selectedPage.ArmorEffectValue)}\n" +
                            $"✦ Skill:  {PageData.GetEffectDisplay(selectedPage.SkillEffect, selectedPage.SkillEffectValue)}";
      tooltipPanel.Visible = true;

      tooltipPanel.Modulate = new Color(1, 1, 1, 0);
      Tween ttTween = CreateTween();
      ttTween.TweenProperty(tooltipPanel, "modulate", new Color(1, 1, 1, 1), 0.2f)
        .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    } else {
      tooltipPanel.Visible = false;
    }
  }

  private void OnPageClicked(PageData page) {
    if (page == null || socketComponent == null || inventory == null) return;

    if (selectedPageForSocket == page) {
      selectedPageForSocket = null;
    } else {
      selectedPageForSocket = page;
    }

    RefreshUI();
  }

  private void OnSlotClicked(string category, int slotIndex) {
    if (socketComponent == null || inventory == null) return;

    SocketSlot slot = GetSlotByCategoryIndex(category, slotIndex);

    if (selectedPageForSocket != null) {
      if (slot != null && slot.HasPage) return;

      // Remove from old socket if already socketed somewhere
      string oldCategory = GetCurrentSocketCategory(selectedPageForSocket);
      if (oldCategory != null) {
        socketComponent.RemovePage(selectedPageForSocket, oldCategory);
        PlaySocketTimeline(false); // Unequip
        // Don't add back to inventory - we're moving it directly
      } else {
        // Remove from inventory only if it wasn't socketed before
        inventory.RemovePageItem(selectedPageForSocket);
      }

      // Socket into the specific slot the player clicked
      socketComponent.SocketPage(selectedPageForSocket, category, slotIndex);
      PlaySocketTimeline(true); // Equip
      selectedPageForSocket = null;
    } else {
      if (slot != null && slot.HasPage) {
        PageData page = slot.CurrentPage;
        socketComponent.RemovePage(page, category);
        PlaySocketTimeline(false); // Unequip
        inventory.AddPageItem(page);
      }
    }

    RefreshUI();
  }

private void PlaySocketTimeline(bool isEquip) {
   
    var socketEvent = FmodServerWrapper.CreateEventInstance("event:/UI_Pages_Sound_Timeline");
    
    if (socketEvent != null) {
    
        socketEvent.SetParameterByName("OnEquip", isEquip ? 1.0f : 0.0f);
        
 
        socketEvent.Start();
        
    }
}

  private SocketSlot GetSlotByCategoryIndex(string category, int index) {
    List<SocketSlot> slots = category switch {
      "Weapon" => weaponSlots,
      "Armor" => armorSlots,
      "Skill" => skillSlots,
      _ => null
    };
    if (slots == null || index < 0 || index >= slots.Count) return null;
    return slots[index];
  }

  private string GetCurrentSocketCategory(PageData page) {
    if (socketComponent == null) return null;
    Dictionary<string, PageData> socketed = socketComponent.GetAllSocketedPages();
    foreach (var kvp in socketed) {
      if (kvp.Value == page) {
        string slotId = kvp.Key;
        return slotId.Split('_')[0];
      }
    }
    return null;
  }

  private void OnClosePressed() {
    Close();
  }
}

/// <summary>
/// A single socket slot with gothic engraved frame styling.
/// Empty slots look like ancient carved sockets; filled slots glow.
/// </summary>
public partial class SocketSlot : MarginContainer {
  private Button slotButton;
  private Label slotLabel;
  private Label pageNameLabel;

  public PageData CurrentPage { get; private set; }
  public bool HasPage => CurrentPage != null;
  public string Category { get; private set; }
  public int SlotIndex { get; private set; }

  public event System.Action<string, int> SlotClicked;

  public SocketSlot(string category, int index) {
    Category = category;
    SlotIndex = index;
    BuildSlot();
  }

  private void BuildSlot() {
    SizeFlagsHorizontal = SizeFlags.ExpandFill;
    CustomMinimumSize = new Vector2(0, 36);

    // Use the button as the main clickable container
    slotButton = new Button();
    slotButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    slotButton.SizeFlagsVertical = SizeFlags.ExpandFill;
    slotButton.Flat = true;
    slotButton.Pressed += () => {
      SlotClicked?.Invoke(Category, SlotIndex);
    };
    AddChild(slotButton);

    // Style the button like an engraved frame
    slotButton.AddThemeStyleboxOverride("normal", GothicTheme.CreateFrameStyle(
      new Color(0.08f, 0.07f, 0.06f, 0.90f),
      GothicTheme.ColorDarkBronze, 2, 4
    ));
    slotButton.AddThemeStyleboxOverride("hover", GothicTheme.CreateFrameStyle(
      new Color(0.10f, 0.09f, 0.07f, 0.95f),
      GothicTheme.ColorAncientGold, 2, 4
    ));
    slotButton.AddThemeStyleboxOverride("pressed", GothicTheme.CreateFrameStyle(
      new Color(0.12f, 0.10f, 0.06f, 0.95f),
      GothicTheme.ColorBrightGold, 2, 4
    ));

    HBoxContainer hbox = new HBoxContainer();
    hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
    slotButton.AddChild(hbox);

    slotLabel = new Label();
    slotLabel.Text = "◻";
    slotLabel.CustomMinimumSize = new Vector2(24, 0);
    slotLabel.HorizontalAlignment = HorizontalAlignment.Center;
    slotLabel.Theme = GothicTheme.CreateLabelTheme(14, GothicTheme.ColorDarkBronze);
    hbox.AddChild(slotLabel);

    pageNameLabel = new Label();
    pageNameLabel.Text = $"[ {Category} {SlotIndex} ]";
    pageNameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    pageNameLabel.Theme = GothicTheme.CreateLabelTheme(13, new Color(0.40f, 0.35f, 0.25f, 1.0f));
    hbox.AddChild(pageNameLabel);

    slotButton.MouseEntered += OnHoverStart;
    slotButton.MouseExited += OnHoverEnd;
  }

  private void OnHoverStart() {
    slotButton.AddThemeStyleboxOverride("normal", GothicTheme.CreateFrameStyle(
      HasPage ? new Color(0.15f, 0.12f, 0.08f, 0.95f) : new Color(0.10f, 0.09f, 0.07f, 0.95f),
      GothicTheme.ColorAncientGold, 2, 4
    ));
    Tween tween = CreateTween();
    tween.TweenProperty(slotButton, "scale", new Vector2(1.03f, 1.03f), 0.1f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
  }

  private void OnHoverEnd() {
    slotButton.AddThemeStyleboxOverride("normal", GothicTheme.CreateFrameStyle(
      new Color(0.08f, 0.07f, 0.06f, 0.90f),
      GothicTheme.ColorDarkBronze, 2, 4
    ));
    Tween tween = CreateTween();
    tween.TweenProperty(slotButton, "scale", Vector2.One, 0.1f)
      .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
  }

  public void SetPage(PageData page) {
    CurrentPage = page;
    if (page != null) {
      pageNameLabel.Text = page.PageName;
      pageNameLabel.Theme = GothicTheme.CreateLabelTheme(13, GothicTheme.ColorBrightGold);
      slotLabel.Text = "◆";
      slotLabel.Theme = GothicTheme.CreateLabelTheme(14, GothicTheme.ColorAncientGold);
      slotButton.AddThemeStyleboxOverride("normal", GothicTheme.CreateFrameStyle(
        new Color(0.12f, 0.10f, 0.06f, 0.95f),
        GothicTheme.ColorAncientGold, 2, 4
      ));
    } else {
      ClearPage();
    }
  }

  public void ClearPage() {
    CurrentPage = null;
    pageNameLabel.Text = $"[ {Category} {SlotIndex} ]";
    pageNameLabel.Theme = GothicTheme.CreateLabelTheme(13, new Color(0.40f, 0.35f, 0.25f, 1.0f));
    slotLabel.Text = "◻";
    slotLabel.Theme = GothicTheme.CreateLabelTheme(14, GothicTheme.ColorDarkBronze);
    slotButton.AddThemeStyleboxOverride("normal", GothicTheme.CreateFrameStyle(
      new Color(0.08f, 0.07f, 0.06f, 0.90f),
      GothicTheme.ColorDarkBronze, 2, 4
    ));
  }
}
