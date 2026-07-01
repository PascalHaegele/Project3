using Godot;
using System.Collections.Generic;

// Tracks a collection of items held by an actor
[GlobalClass]
public partial class InventoryComponent : Node {
  // List of unique item identifiers
  public List<string> Items { get; private set; } = new();

  // Signal emitted whenever the inventory content changes
  [Signal]
  public delegate void InventoryChangedEventHandler();

  // Adds an item if it does not exist and notifies listeners
  public void AddItem(string itemName) {
    if(Items.Contains(itemName)) { return; }

    Items.Add(itemName);

    GD.Print($"Item Added: {itemName}");

    _ = EmitSignal(SignalName.InventoryChanged);
  }

  // Returns true if the item is present in the inventory
  public bool HasItem(string itemName) => Items.Contains(itemName);

  // Removes an item and notifies listeners if successful
  public bool RemoveItem(string itemName) {
    bool removed = Items.Remove(itemName);

    if(removed) {
      GD.Print($"Item Removed: {itemName}");
      _ = EmitSignal(SignalName.InventoryChanged);
    }

    return removed;
  }

  // Removes all items from the inventory
  public void ClearInventory() {
    Items.Clear();

    GD.Print("Inventory Cleared");

    _ = EmitSignal(SignalName.InventoryChanged);
  }

  // Prints the current inventory content to the console
  public void PrintInventory() {
    GD.Print("------ Inventory ------");

    if(Items.Count == 0) {
      GD.Print("Inventory is Empty");
      return;
    }

    foreach(string item in Items) { GD.Print(item); }
  }
}

