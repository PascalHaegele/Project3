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

  /// <summary>
  /// Returns a human-readable description of an effect name.
  /// </summary>
  public static string GetEffectDescription(string effectName) {
    return effectName switch {
      "Damage" => "Increases weapon damage",
      "ReloadSpeed" => "Increases reload speed",
      "HomingProjectiles" => "Projectiles track enemies",
      "MovementSpeed" => "Increases movement speed",
      "DamageReduction" => "Reduces damage taken",
      "DashDistance" => "Increases dash distance",
      "BloodRitual" => "Heal on hit",
      "EchoStep" => "Leaves a damaging shadow trail",
      "FrenziedSoul" => "Increases fire rate",
      _ => effectName
    };
  }

  /// <summary>
  /// Returns a formatted string showing the effect with its description and value.
  /// </summary>
  public static string GetEffectDisplay(string effectName, float effectValue) {
    string desc = GetEffectDescription(effectName);
    if (effectName == "HomingProjectiles" || effectName == "EchoStep") {
      return $"{desc}";
    }
    return $"{desc} (+{effectValue})";
  }
}
