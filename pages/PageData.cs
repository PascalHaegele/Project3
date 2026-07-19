using Godot;

/// <summary>
/// Represents a single equippable Page (modifier card) in the Pages Upgrade System.
/// Each Page has a name, description, and THREE different effects:
/// - Weapon Effect (applied when socketed into Weapon Socket)
/// - Armor Effect (applied when socketed into Armor Socket)  
/// - Skill Effect (applied when socketed into Skill Socket)
/// </summary>
[GlobalClass]
public partial class PageData : Resource {
  /// <summary>
  /// Display name of the page (e.g. "Frenzied Soul").
  /// </summary>
  [Export] public string PageName { get; set; } = "Unnamed Page";

  /// <summary>
  /// Flavor / gameplay description shown in the UI tooltip.
  /// </summary>
  [Export] public string Description { get; set; } = "";

  /// <summary>
  /// Effect applied when socketed into a Weapon Socket.
  /// </summary>
  [Export] public string WeaponEffect { get; set; } = "";
  [Export] public float WeaponEffectValue { get; set; } = 0f;

  /// <summary>
  /// Effect applied when socketed into an Armor Socket.
  /// </summary>
  [Export] public string ArmorEffect { get; set; } = "";
  [Export] public float ArmorEffectValue { get; set; } = 0f;

  /// <summary>
  /// Effect applied when socketed into a Skill Socket.
  /// </summary>
  [Export] public string SkillEffect { get; set; } = "";
  [Export] public float SkillEffectValue { get; set; } = 0f;

  /// <summary>
  /// Rarity tier for display purposes.
  /// </summary>
  [Export] public string Rarity { get; set; } = "Common";

  /// <summary>
  /// Returns the effect name and value for a given socket category.
  /// </summary>
  public (string EffectName, float EffectValue) GetEffectForCategory(string category) {
    return category switch {
      "Weapon" => (WeaponEffect, WeaponEffectValue),
      "Armor" => (ArmorEffect, ArmorEffectValue),
      "Skill" => (SkillEffect, SkillEffectValue),
      _ => ("", 0f)
    };
  }
}