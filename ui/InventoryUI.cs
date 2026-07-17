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
  private Label currencyLabel;
  private Label weaponNameLabel;
  private Button closeButton;

  private Label tooltipName;
  private Label tooltipDescription;
  private Label tooltipCategory;
  private Label tooltipModifier;
  private Label tooltipValue;
  private Label tooltipRarity;

  private PageData selectedPage;
  private PageData draggedPage;

  private List<SocketSlot> weaponSlots = new();
  private List<SocketSlot> armorSlots = new();
  private List<SocketSlot> skillSlots = new();

  [Signal] public delegate void InventoryClosedEventHandler();

  public override void _Ready() {
    Visible = false;
    GD.Print("InventoryUI._Ready");

    weaponSocketContainer = GetNodeOrNull<VBoxContainer>("MarginContainer/HBoxContainer/SocketPanel/VBoxContainer/WeaponSockets/WeaponSocketContainer");
    armorSocketContainer = GetNodeOrNull<VBoxContainer>("MarginContainer/HBoxContainer/SocketPanel/VBoxContainer/ArmorSockets/ArmorSocketContainer");
    skillSocketContainer = GetNodeOrNull<VBoxContainer>("MarginContainer/HBoxContainer/SocketPanel/VBoxContainer/SkillSockets/SkillSocketContainer");
    pagesListContainer = GetNodeOrNull<VBoxContainer>("MarginContainer/HBoxContainer/PagesPanel/VBoxContainer/ScrollContainer/PagesList");
    tooltipPanel = GetNodeOrNull<PanelContainer>("MarginContainer/HBoxContainer/PagesPanel/VBoxContainer/TooltipPanel");
    ammoLabel = GetNodeOrNull<Label>("MarginContainer/HBoxContainer/StatsPanel/VBoxContainer/AmmoCount");
    potionLabel = GetNodeOrNull<Label>("MarginContainer/HBoxContainer/StatsPanel/VBoxContainer/PotionCount");
    currencyLabel = GetNodeOrNull<Label>("MarginContainer/HBoxContainer/StatsPanel/VBoxContainer/CurrencyCount");
    weaponNameLabel = GetNodeOrNull<Label>("MarginContainer/HBoxContainer/StatsPanel/VBoxContainer/WeaponName");
    closeButton = GetNodeOrNull<Button>("MarginContainer/HBoxContainer/StatsPanel/VBoxContainer/CloseButton");

    if (tooltipPanel != null) {
      VBoxContainer tooltipVBox = tooltipPanel.GetNodeOrNull<VBoxContainer>("VBoxContainer");
      if (tooltipVBox != null) {
        tooltipName = tooltipVBox.GetNodeOrNull<Label>("NameLabel");
        tooltipDescription = tooltipVBox.GetNodeOrNull<Label>("DescriptionLabel");
        tooltipCategory = tooltipVBox.GetNodeOrNull<Label>("CategoryLabel");
        tooltipModifier = tooltipVBox.GetNodeOrNull<Label>("ModifierLabel");
        tooltipValue = tooltipVBox.GetNodeOrNull<Label>("ValueLabel");
        tooltipRarity = tooltipVBox.GetNodeOrNull<Label>("RarityLabel");
      }
    }

    CreateSocketSlots();

    if (closeButton != null) {
      closeButton.Pressed += OnClosePressed;
    }
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
      
      // Show all three effects in the button text
      pageButton.Text = $"{page.PageName} [{page.Rarity}]\n" +
                       $"  W: {page.WeaponEffect} {page.WeaponEffectValue}\n" +
                       $"  A: {page.ArmorEffect} {page.ArmorEffectValue}\n" +
                       $"  S: {page.SkillEffect} {page.SkillEffectValue}";
      pageButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
      pageButton.CustomMinimumSize = new Vector2(0, 60);

      PageData captured = page;
      pageButton.MouseEntered += () => OnPageHovered(captured);
      
      // Store reference for manual drag handling
      pageButton.MouseFilter = Control.MouseFilterEnum.Stop;
      pageButton.Pressed += () => {
        draggedPage = captured;
        selectedPage = captured;
        RefreshTooltip();
      };
      
      pagesListContainer.AddChild(pageButton);
    }

    if (!hasAny) {
      Label emptyLabel = new Label();
      emptyLabel.Text = "No pages available.";
      emptyLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
      pagesListContainer.AddChild(emptyLabel);
    }
  }

  private void CreateSocketSlots() {
    if (weaponSocketContainer == null || armorSocketContainer == null || skillSocketContainer == null) return;

    for (int i = 0; i < 4; i++) {
      SocketSlot slot = new SocketSlot("Weapon", i);
      slot.RemoveRequested += OnRemoveFromSocket;
      weaponSlots.Add(slot);
      weaponSocketContainer.AddChild(slot);
    }
    for (int i = 0; i < 3; i++) {
      SocketSlot slot = new SocketSlot("Armor", i);
      slot.RemoveRequested += OnRemoveFromSocket;
      armorSlots.Add(slot);
      armorSocketContainer.AddChild(slot);
    }
    for (int i = 0; i < 3; i++) {
      SocketSlot slot = new SocketSlot("Skill", i);
      slot.RemoveRequested += OnRemoveFromSocket;
      skillSlots.Add(slot);
      skillSocketContainer.AddChild(slot);
    }
  }

  private void RefreshSocketSlots() {
    foreach (SocketSlot slot in weaponSlots) slot.ClearPage();
    foreach (SocketSlot slot in armorSlots) slot.ClearPage();
    foreach (SocketSlot slot in skillSlots) slot.ClearPage();

    if (socketComponent == null) return;

    // Get socketed pages from SocketComponent
    Dictionary<string, PageData> socketed = socketComponent.GetAllSocketedPages();
    
    // Group by category
    int weaponIdx = 0, armorIdx = 0, skillIdx = 0;
    foreach (var kvp in socketed) {
      PageData page = kvp.Value;
      string slotId = kvp.Key;
      
      // Determine category from slot ID (format: "Category_index")
      string category = slotId.Split('_')[0];
      
      switch (category) {
        case "Weapon":
          if (weaponIdx < weaponSlots.Count) {
            weaponSlots[weaponIdx].SetPage(page);
            weaponIdx++;
          }
          break;
        case "Armor":
          if (armorIdx < armorSlots.Count) {
            armorSlots[armorIdx].SetPage(page);
            armorIdx++;
          }
          break;
        case "Skill":
          if (skillIdx < skillSlots.Count) {
            skillSlots[skillIdx].SetPage(page);
            skillIdx++;
          }
          break;
      }
    }
  }

  private void RefreshStats() {
    if (inventory == null) return;
    int potions = inventory.items[(int)ItemType.POTION];
    potionLabel.Text = $"Potions: {potions}";
    currencyLabel.Text = $"Currency: 0";

    if (weapon != null) {
      weaponNameLabel.Text = $"Weapon: {weapon.Name}";
      ammoLabel.Text = $"Ammo: {weapon.CurrentAmmo} / {weapon.info.magazineSize}";
    }
  }

  private void RefreshTooltip() {
    if (tooltipPanel == null) return;
    if (selectedPage != null) {
      tooltipName.Text = selectedPage.PageName;
      tooltipDescription.Text = selectedPage.Description;
      tooltipCategory.Text = $"Rarity: {selectedPage.Rarity}";
      tooltipModifier.Text = $"Weapon: {selectedPage.WeaponEffect} {selectedPage.WeaponEffectValue}\n" +
                            $"Armor: {selectedPage.ArmorEffect} {selectedPage.ArmorEffectValue}\n" +
                            $"Skill: {selectedPage.SkillEffect} {selectedPage.SkillEffectValue}";
      tooltipValue.Text = "";
      tooltipRarity.Text = "";
      
      // Get currently active effect
      string activeEffect = GetActiveEffect(selectedPage);
      tooltipDescription.Text += $"\n\nCurrently Active: {activeEffect}";
      
      tooltipPanel.Visible = true;
    } else {
      tooltipPanel.Visible = false;
    }
  }

  private string GetActiveEffect(PageData page) {
    if (socketComponent == null) return "None (not socketed)";
    
    // Check which category this page is socketed in
    Dictionary<string, PageData> socketed = socketComponent.GetAllSocketedPages();
    foreach (var kvp in socketed) {
      if (kvp.Value == page) {
        string category = kvp.Key.Split('_')[0];
        (string effect, float value) = page.GetEffectForCategory(category);
        return $"{category}: {effect} {value}";
      }
    }
    
    return "Not socketed";
  }

  private void OnPageHovered(PageData page) {
    selectedPage = page;
    RefreshTooltip();
  }

  private string GetCategoryFromPosition(Vector2 atPosition) {
    Control slot = GetNodeOrNull<Control>("MarginContainer/HBoxContainer/SocketPanel/VBoxContainer/WeaponSockets/WeaponSocketContainer");
    if (slot != null && slot.GetGlobalRect().HasPoint(atPosition)) return "Weapon";
    
    slot = GetNodeOrNull<Control>("MarginContainer/HBoxContainer/SocketPanel/VBoxContainer/ArmorSockets/ArmorSocketContainer");
    if (slot != null && slot.GetGlobalRect().HasPoint(atPosition)) return "Armor";
    
    slot = GetNodeOrNull<Control>("MarginContainer/HBoxContainer/SocketPanel/VBoxContainer/SkillSockets/SkillSocketContainer");
    if (slot != null && slot.GetGlobalRect().HasPoint(atPosition)) return "Skill";
    
    return null;
  }

  public override void _Input(InputEvent @event) {
    if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
      if (draggedPage != null && socketComponent != null && inventory != null) {
        // Get global mouse position
        Vector2 mousePos = GetGlobalMousePosition();
        
        // Check if dropped on a socket category
        string category = GetCategoryFromPosition(mousePos);
        if (category != null) {
          // Check if page is already socketed - remove from old socket first
          string oldCategory = GetCurrentSocketCategory(draggedPage);
          if (oldCategory != null) {
            socketComponent.RemovePage(draggedPage, oldCategory);
            inventory.AddPageItem(draggedPage);
          }

          // Get first empty slot of this category
          SocketSlot slot = GetFirstEmptySlot(category);
          if (slot != null) {
            // Socket into new category
            inventory.RemovePageItem(draggedPage);
            socketComponent.SocketPage(draggedPage, category);
            slot.SetPage(draggedPage);
            
            (string effect, float value) = draggedPage.GetEffectForCategory(category);
            GD.Print($">>> Drag-Dropped {draggedPage.PageName} into {category}. Effect: {effect} = {value}");
          } else {
            GD.Print($"No empty {category} slots available!");
          }

          RefreshUI();
          draggedPage = null;
          return;
        }
      }
    }
  }

  private void OnPageClicked(PageData page) {
    if (page == null) return;

    // Cycle through socket categories: Weapon -> Armor -> Skill -> None
    string nextCategory = GetNextCategory(page);
    
    if (nextCategory == null) {
      // Page is already socketed, remove it
      string currentCategory = GetCurrentSocketCategory(page);
      if (currentCategory != null) {
        socketComponent.RemovePage(page, currentCategory);
        inventory.AddPageItem(page);
        GD.Print($"Removed {page.PageName} from {currentCategory} socket.");
      }
    } else {
      // Socket into next available slot of this category
      SocketSlot slot = GetFirstEmptySlot(nextCategory);
      if (slot != null && socketComponent != null && inventory != null) {
        // Check if page is already socketed - remove from old socket first
        string oldCategory = GetCurrentSocketCategory(page);
        if (oldCategory != null) {
          socketComponent.RemovePage(page, oldCategory);
          inventory.AddPageItem(page);
        }
        
        // Now socket into new category
        inventory.RemovePageItem(page);
        socketComponent.SocketPage(page, nextCategory);
        slot.SetPage(page);
        
        (string effect, float value) = page.GetEffectForCategory(nextCategory);
        GD.Print($">>> Socketed {page.PageName} into {nextCategory}. Effect: {effect} = {value}");
      } else {
        GD.Print($"No empty {nextCategory} slots available!");
      }
    }

    RefreshUI();
  }

  private string GetNextCategory(PageData page) {
    string currentCategory = GetCurrentSocketCategory(page);
    
    // If not socketed, default to Weapon
    if (currentCategory == null) {
      return "Weapon";
    }
    
    // Cycle: Weapon -> Armor -> Skill -> null (removal)
    return currentCategory switch {
      "Weapon" => "Armor",
      "Armor" => "Skill",
      "Skill" => null, // Will trigger removal
      _ => "Weapon"
    };
  }

  private string GetCurrentSocketCategory(PageData page) {
    if (socketComponent == null) return null;
    
    Dictionary<string, PageData> socketed = socketComponent.GetAllSocketedPages();
    foreach (var kvp in socketed) {
      if (kvp.Value == page) {
        return kvp.Key.Split('_')[0];
      }
    }
    return null;
  }

  private SocketSlot GetFirstEmptySlot(string category) {
    List<SocketSlot> slots = category switch {
      "Weapon" => weaponSlots,
      "Armor" => armorSlots,
      "Skill" => skillSlots,
      _ => null
    };

    if (slots == null) return null;

    foreach (SocketSlot slot in slots) {
      if (!slot.HasPage) return slot;
    }
    return null;
  }

  private void OnRemoveFromSocket(PageData page) {
    if (page == null || socketComponent == null) return;

    string category = GetCurrentSocketCategory(page);
    if (category != null) {
      socketComponent.RemovePage(page, category);
      inventory.AddPageItem(page);
      GD.Print($"Removed {page.PageName} from {category} socket.");
    }

    RefreshUI();
  }

  private List<SocketSlot> GetSlotsForCategory(string category) {
    return category switch {
      "Weapon" => weaponSlots,
      "Armor" => armorSlots,
      "Skill" => skillSlots,
      _ => new List<SocketSlot>()
    };
  }

  private void OnClosePressed() {
    Close();
  }
}

public partial class SocketSlot : MarginContainer {
  private Button slotButton;
  private Button removeButton;

  public PageData CurrentPage { get; private set; }
  public bool HasPage => CurrentPage != null;
  public string Category { get; private set; }
  public int SlotIndex { get; private set; }

  public event System.Action<PageData> RemoveRequested;

  public SocketSlot(string category, int index) {
    Category = category;
    SlotIndex = index;
    BuildSlot();
  }

  private void BuildSlot() {
    SizeFlagsHorizontal = SizeFlags.ExpandFill;
    CustomMinimumSize = new Vector2(0, 40);

    HBoxContainer hbox = new HBoxContainer();
    hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    AddChild(hbox);

    slotButton = new Button();
    slotButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    slotButton.Text = "[ Empty ]";
    slotButton.Disabled = true;
    hbox.AddChild(slotButton);

    removeButton = new Button();
    removeButton.Text = "X";
    removeButton.CustomMinimumSize = new Vector2(30, 0);
    removeButton.Visible = false;
    removeButton.Pressed += () => RemoveRequested?.Invoke(CurrentPage);
    hbox.AddChild(removeButton);
  }

  public void SetPage(PageData page) {
    CurrentPage = page;
    slotButton.Text = $"{page.PageName}";
    slotButton.Disabled = false;
    removeButton.Visible = true;
  }

  public void ClearPage() {
    CurrentPage = null;
    slotButton.Text = "[ Empty ]";
    slotButton.Disabled = true;
    removeButton.Visible = false;
  }
}