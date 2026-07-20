using Godot;
using System.Collections.Generic;

public enum ItemType { Potion, RAmmo, SAmmo, Page, Max, }

// Tracks a collection of items held by an actor
[GlobalClass]
public partial class InventoryComponent : Node {
  public readonly int[] maxItems = [3, 54, 36, 100];
  public readonly int[] items = new int[(int)ItemType.Max];
  public readonly string[] itemNames = [
    "Potion",
    "Revolver Ammunition",
    "Shotgun Ammunition",
    "Page"
  ];

  /// <summary>
  /// Stores the actual PageData resources the player has collected.
  /// </summary>
  public List<PageData> collectedPages = new();

  // Signal emitted whenever the inventory content changes
  [Signal] public delegate void InventoryChangedEventHandler();

  public override void _Ready() {
    items[(int)ItemType.RAmmo] = maxItems[(int)ItemType.RAmmo];
    items[(int)ItemType.SAmmo] = maxItems[(int)ItemType.SAmmo];
  }

  public void Reset() {
    ClearInventory();
    items[(int)ItemType.RAmmo] = maxItems[(int)ItemType.RAmmo];
    items[(int)ItemType.SAmmo] = maxItems[(int)ItemType.SAmmo];
  }

  // Adds an item if it does not exist and notifies listeners
  public void AddItem(ItemType type, int amount = 1) {
    if(items[(int)type] >= maxItems[(int)type]) { return; }

    items[(int)type] =
      Mathf.Min(maxItems[(int)type], items[(int)type] + amount);

    GD.Print($"Item Added: {type}");

    EmitSignalInventoryChanged();
  }

  /// <summary>
  /// Adds a PageData directly to the collected pages list.
  /// Used by LootComponent when dropping pages.
  /// </summary>
  public void AddPageItem(PageData page) {
    collectedPages.Add(page);
    GD.Print($"Page Collected: {page.PageName}");
    EmitSignalInventoryChanged();
  }

  /// <summary>
  /// Removes a specific PageData from the collected pages list.
  /// Used when socketing a page into a slot.
  /// </summary>
  public bool RemovePageItem(PageData page) {
    bool removed = collectedPages.Remove(page);
    if(removed) {
      GD.Print($"Page Removed from inventory: {page.PageName}");
      EmitSignalInventoryChanged();
    }
    return removed;
  }

  /// <summary>
  /// Checks if the player owns a specific page by name.
  /// </summary>
  public bool HasPage(string pageName) {
    foreach(PageData p in collectedPages) {
      if(p.PageName == pageName) { return true; }
    }
    return false;
  }

  // Returns true if the item is present in the inventory
  public bool HasItem(ItemType type) => items[(int)type] > 0;

  public int AmountOf(ItemType type) => items[(int)type];

  // Removes an item and notifies listeners if successful
  public bool RemoveItem(ItemType type) {
    bool removed = HasItem(type);

    if(removed) {
      items[(int)type]--;
      GD.Print($"Item Removed: {type}");
      EmitSignalInventoryChanged();
    }

    return removed;
  }

  public int RemoveItem(ItemType type, int amount = 1) {
    bool removed = HasItem(type);
    int amountRemoved = 0;

    if(removed) {
      amountRemoved = items[(int)type] < amount ? items[(int)type] : amount;
      items[(int)type] -= amountRemoved;
      GD.Print($"Item Removed: {type}, amount: {amountRemoved}");
      EmitSignalInventoryChanged();
    }

    return amountRemoved;
  }

  // Removes all items from the inventory
  public void ClearInventory() {
    for(int i = 0; i < items.Length; i++) { items[i] = 0; }
    collectedPages.Clear();
    GD.Print("Inventory Cleared");
    EmitSignalInventoryChanged();
  }

  // Prints the current inventory content to the console
  public void PrintInventory() {
    GD.Print("------ Inventory ------");
    for(int i = 0; i < items.Length; i++) {
      GD.Print($"{itemNames[i]}: {items[i]}");
    }
    GD.Print($"Pages Collected: {collectedPages.Count}");
    foreach(PageData p in collectedPages) {
      GD.Print($"  - {p.PageName} [{p.Rarity}]");
    }
  }
}

