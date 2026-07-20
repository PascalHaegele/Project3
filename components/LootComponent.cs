using Godot;
using System.Collections.Generic;

// Configures drop tables and handles randomized loot generation
[GlobalClass]
public partial class LootComponent : Node {
  // Array of potential items that can be dropped
  [Export]
  public Godot.Collections.Array<string> PossiblePages = new();

  // Array of additional item types that can drop (ammo, health, currency)
  [Export] public bool CanDropAmmo = false;
  [Export] public bool CanDropHealth = false;
  [Export] public bool CanDropCurrency = false;

  // Random number generator used for loot selection
  private RandomNumberGenerator rng = new();

  // Signal emitted whenever loot is successfully generated
  [Signal] public delegate void LootGeneratedEventHandler(string itemName);

  /// <summary>
  /// Signal emitted when a PageData should be added to the player's inventory.
  /// The receiving system (e.g. pickup or direct injection) handles the actual addition.
  /// </summary>
  [Signal] public delegate void PageDroppedEventHandler(PageData page);

  public override void _Ready() { rng.Randomize(); }

  /// <summary>
  /// Returns a random page name from the possible pages list.
  /// Legacy method kept for backward compatibility.
  /// </summary>
  public string GenerateLoot() {
    if(PossiblePages.Count == 0) {
      GD.Print("LootComponent: No loot available.");
      return "";
    }

    int index = rng.RandiRange(0, PossiblePages.Count - 1);
    string item = PossiblePages[index];

    GD.Print($"Loot Generated: {item}");

    _ = EmitSignal(SignalName.LootGenerated, item);

    return item;
  }

  /// <summary>
  /// Generates a random PageData from the possible pages list.
  /// Returns null if no pages are configured.
  /// </summary>
  public PageData GeneratePageDrop() {
    // Always return a random page from the database for testing
    if(PageDatabase.PageCount > 0) {
      int randomIndex = rng.RandiRange(0, PageDatabase.PageCount - 1);
      string[] allPages = [.. PageDatabase.GetAllPageNames()];
      if(allPages.Length > 0) {
        PageData page = PageDatabase.GetPage(allPages[randomIndex]);
        if(page != null) {
          GD.Print($"Page Drop Generated: {page.PageName}");
          _ = EmitSignal(SignalName.PageDropped, page);
          return page;
        }
      }
    }

    GD.Print("LootComponent: No pages available for drop.");
    return null;
  }

  /// <summary>
  /// Generates a random non-page loot type (ammo, health, currency).
  /// Returns the ItemType or null if nothing drops.
  /// </summary>
  public ItemType? GenerateItemDrop() {
    // Simple random selection among enabled non-page drops
    List<ItemType> possibleDrops = new();
    if(CanDropAmmo) { possibleDrops.Add(ItemType.R_AMMO); }
    if(CanDropHealth) { possibleDrops.Add(ItemType.POTION); }

    // Currency would need a new ItemType if needed

    if(possibleDrops.Count == 0) { return null; }

    int index = rng.RandiRange(0, possibleDrops.Count - 1);
    return possibleDrops[index];
  }

  // Adds an item to the loot table if it is not already present
  public void AddPossiblePage(string pageName) {
    if(!PossiblePages.Contains(pageName)) {
      PossiblePages.Add(pageName);
      GD.Print($"Added possible loot: {pageName}");
    }
  }

  // Removes an item from the loot table if it exists
  public void RemovePossiblePage(string pageName) {
    if(PossiblePages.Contains(pageName)) {
      _ = PossiblePages.Remove(pageName);
      GD.Print($"Removed possible loot: {pageName}");
    }
  }

  // Returns the total number of items currently in the loot table
  public int GetLootCount() => PossiblePages.Count;
}

