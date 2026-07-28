using Godot;

/// <summary>
/// The three collectible currency types.
/// </summary>
public enum CurrencyType {
  EchoFragment,
  AncientGlyph,
  ForbiddenEssence
}

/// <summary>
/// Defines a currency type with display name, rarity, and linked GLB model.
/// </summary>
[GlobalClass]
public partial class CurrencyData : Resource {
  [Export] public string CurrencyName { get; set; } = "Currency";
  [Export] public string DisplayName { get; set; } = "Currency";
  [Export] public string Rarity { get; set; } = "Common";
  [Export] public PackedScene ModelScene { get; set; }
}
