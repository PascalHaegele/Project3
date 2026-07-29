using Godot;

/// <summary>
/// Tracks permanent upgrade bonuses on the player.
/// </summary>
[GlobalClass]
public partial class UpgradeTracker : Node {
  public float weaponDamageBonus = 0f;
  public float reloadSpeedBonus = 0f;
  public int randomPageUpgrades = 0;
}

