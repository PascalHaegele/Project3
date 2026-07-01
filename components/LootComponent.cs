using Godot;

// Configures drop tables and handles randomized loot generation
[GlobalClass]
public partial class LootComponent : Node {
  // Array of potential items that can be dropped
  [Export]
  public Godot.Collections.Array<string> PossiblePages = new();

  // Random number generator used for loot selection
  private RandomNumberGenerator rng = new();

  // Signal emitted whenever loot is successfully generated
  [Signal]
  public delegate void LootGeneratedEventHandler(string itemName);

  public override void _Ready() {
    rng.Randomize();
  }

  // Returns a random item based on the defined drop table
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

