using Godot;
using System.Collections.Generic;

/// <summary>
/// Full-screen Inventory UI.
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

  private List<SocketSlot> weaponSlots = new();
  private List<SocketSlot> armorSlots = new();
  private List<SocketSlot> skillSlots = new();
  private PageData selectedPage; // For hover tooltip
  private PageData selectedPageForSocket; // For socket placement

  [Signal] public delegate void InventoryClosedEventHandler();

  public override void _Ready() {
    Visible = false;

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

    GD.Print("InventoryUI: Building UI programmatically");
    AnchorRight = 1.0f;
    AnchorBottom = 1.0f;

    ColorRect bg = new ColorRect();
    bg.Color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    bg.AnchorRight = 1.0f;
    bg.AnchorBottom = 1.0f;
    AddChild(bg);

    HBoxContainer mainHBox = new HBoxContainer();
    mainHBox.AnchorRight = 1.0f;
    mainHBox.AnchorBottom = 1.0f;
    AddChild(mainHBox);

    VBoxContainer socketPanel = new VBoxContainer();
    socketPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    mainHBox.AddChild(socketPanel);

    Label title = new Label();
    title.Text = "SOCKETS";
    title.Theme = CreateLabelTheme(24);
    socketPanel.AddChild(title);

    weaponSocketContainer = new VBoxContainer();
    weaponSocketContainer.Name = "WeaponSocketContainer";
    Label weaponLabel = new Label();
    weaponLabel.Text = "WEAPON SOCKETS";
    weaponLabel.Theme = CreateLabelTheme(18);
    socketPanel.AddChild(weaponLabel);
    socketPanel.AddChild(weaponSocketContainer);

    armorSocketContainer = new VBoxContainer();
    armorSocketContainer.Name = "ArmorSocketContainer";
    Label armorLabel = new Label();
    armorLabel.Text = "ARMOR SOCKETS";
    armorLabel.Theme = CreateLabelTheme(18);
    socketPanel.AddChild(armorLabel);
    socketPanel.AddChild(armorSocketContainer);

    skillSocketContainer = new VBoxContainer();
    skillSocketContainer.Name = "SkillSocketContainer";
    Label skillLabel = new Label();
    skillLabel.Text = "SKILL SOCKETS";
    skillLabel.Theme = CreateLabelTheme(18);
    socketPanel.AddChild(skillLabel);
    socketPanel.AddChild(skillSocketContainer);

    VBoxContainer statsPanel = new VBoxContainer();
    statsPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    mainHBox.AddChild(statsPanel);

    ammoLabel = new Label();
    ammoLabel.Text = "Ammo: 0/0";
    ammoLabel.Theme = CreateLabelTheme(18);
    statsPanel.AddChild(ammoLabel);

    potionLabel = new Label();
    potionLabel.Text = "Potions: 0";
    potionLabel.Theme = CreateLabelTheme(18);
    statsPanel.AddChild(potionLabel);

    weaponNameLabel = new Label();
    weaponNameLabel.Text = "Weapon: None";
    weaponNameLabel.Theme = CreateLabelTheme(18);
    statsPanel.AddChild(weaponNameLabel);

    closeButton = new Button();
    closeButton.Name = "CloseButton";
    closeButton.Text = "CLOSE (I)";
    closeButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    statsPanel.AddChild(closeButton);

    VBoxContainer pagesPanel = new VBoxContainer();
    pagesPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    mainHBox.AddChild(pagesPanel);

    Label pagesTitle = new Label();
    pagesTitle.Text = "PAGES";
    pagesTitle.Theme = CreateLabelTheme(24);
    pagesPanel.AddChild(pagesTitle);

    ScrollContainer scroll = new ScrollContainer();
    scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
    pagesPanel.AddChild(scroll);

    pagesListContainer = new VBoxContainer();
    pagesListContainer.Name = "PagesList";
    pagesListContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    scroll.AddChild(pagesListContainer);

    tooltipPanel = new PanelContainer();
    tooltipPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    tooltipPanel.CustomMinimumSize = new Vector2(0, 100);
    VBoxContainer tooltipVBox = new VBoxContainer();
    tooltipPanel.AddChild(tooltipVBox);

    tooltipName = new Label();
    tooltipName.Theme = CreateLabelTheme(18);
    tooltipVBox.AddChild(tooltipName);

    tooltipDescription = new Label();
    tooltipDescription.Theme = CreateLabelTheme(14);
    tooltipVBox.AddChild(tooltipDescription);

    tooltipModifier = new Label();
    tooltipModifier.Theme = CreateLabelTheme(14);
    tooltipVBox.AddChild(tooltipModifier);

    pagesPanel.AddChild(tooltipPanel);
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
    Input.MouseMode = Input.MouseModeEnum.Visible;
    RefreshUI();
  }

  public void Close() {
    Visible = false;
    Input.MouseMode = Input.MouseModeEnum.Captured;
    _ = EmitSignal(SignalName.InventoryClosed);
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

      Button pageButton = new Button();
      pageButton.Text = $"{page.PageName} [{page.Rarity}]\n" +
                       $"  W: {page.WeaponEffect} {page.WeaponEffectValue}\n" +
                       $"  A: {page.ArmorEffect} {page.ArmorEffectValue}\n" +
                       $"  S: {page.SkillEffect} {page.SkillEffectValue}";
      pageButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
      pageButton.CustomMinimumSize = new Vector2(0, 60);

      PageData captured = page;
      pageButton.MouseEntered += () => {
        selectedPage = page;
        RefreshTooltip();
      };
      pageButton.Pressed += () => OnPageClicked(captured);
      pagesListContainer.AddChild(pageButton);
    }

    if (!hasAny) {
      Label emptyLabel = new Label();
      emptyLabel.Text = "No pages collected.";
      emptyLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
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
      // Extract slot index from Weaponi (e.g., "Weapon_0" -> 0)
      string[] parts = slotId.Split('_');
      int slotIndex = parts.Length > 1 ? int.Parse(parts[1]) : 0;

      SocketSlot slot = GetSlotByCategoryIndex(category, slotIndex);
      if (slot != null) slot.SetPage(page);
    }
  }

  private void RefreshStats() {
    if (inventory == null) return;
    if (potionLabel != null) potionLabel.Text = $"Potions: {inventory.items[(int)ItemType.Potion]}";
    if (weapon != null && weaponNameLabel != null) weaponNameLabel.Text = $"Weapon: {weapon.Name}";
    if (weapon != null && ammoLabel != null) ammoLabel.Text = $"Ammo: {weapon.CurrentAmmo} / {weapon.info.magazineSize}";
  }

  private void RefreshTooltip() {
    if (tooltipPanel == null) return;
    if (selectedPage != null) {
      tooltipName.Text = selectedPage.PageName;
      tooltipDescription.Text = selectedPage.Description;
      tooltipModifier.Text = $"Weapon: {selectedPage.WeaponEffect} {selectedPage.WeaponEffectValue}\n" +
                            $"Armor: {selectedPage.ArmorEffect} {selectedPage.ArmorEffectValue}\n" +
                            $"Skill: {selectedPage.SkillEffect} {selectedPage.SkillEffectValue}";
      tooltipPanel.Visible = true;
    } else {
      tooltipPanel.Visible = false;
    }
  }

  private void OnPageClicked(PageData page) {
    if (page == null || socketComponent == null || inventory == null) return;

    // Toggle selection: if same page already selected, deselect it
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
      // Try to socket the selected page into this slot
      if (slot != null && slot.HasPage) return; // Slot occupied

      string oldCategory = GetCurrentSocketCategory(selectedPageForSocket);
      if (oldCategory != null) {
        socketComponent.RemovePage(selectedPageForSocket, oldCategory);
        inventory.AddPageItem(selectedPageForSocket);
      }

      inventory.RemovePageItem(selectedPageForSocket);
      socketComponent.SocketPage(selectedPageForSocket, category);
      selectedPageForSocket = null;
    } else {
      // Remove page from this slot if it has one
      if (slot != null && slot.HasPage) {
        PageData page = slot.CurrentPage;
        socketComponent.RemovePage(page, category);
        inventory.AddPageItem(page);
      }
    }

    RefreshUI();
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

public partial class SocketSlot : MarginContainer {
  private Button slotButton;

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
    CustomMinimumSize = new Vector2(0, 40);

    slotButton = new Button();
    slotButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    slotButton.Text = $"[ {Category} {SlotIndex} ] (click to socket)";
    slotButton.Pressed += () => {
      SlotClicked?.Invoke(Category, SlotIndex);
    };
    AddChild(slotButton);
  }

  public void SetPage(PageData page) {
    CurrentPage = page;
    if (page != null) {
      slotButton.Text = $"{Category} {SlotIndex}: {page.PageName}";
    } else {
      slotButton.Text = $"[ {Category} {SlotIndex} ] (click to socket)";
    }
  }

  public void ClearPage() {
    CurrentPage = null;
    slotButton.Text = $"[ {Category} {SlotIndex} ] (click to socket)";
  }
}
