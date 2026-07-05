using Godot;

[GlobalClass]
public partial class WeaponInfo : Resource {
  [Export] public WeaponType type = WeaponType.Crossbow;
  [Export] public float damageMulitplier = 1.0f;
  [Export] public float fireRate = 1.0f;
  [Export] public float reloadTime = 1.5f;
  [Export] public int magazineSize = 1;
  [Export] public int projectileCount = 1;
  [Export] public float projectileSpread = 0.0f;

  [Signal] public delegate void EmptyEventHandler();
}

