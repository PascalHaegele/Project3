using Godot;

public enum WeaponType { Revolver, Shotgun, }

[GlobalClass]
public partial class WeaponInfo : Resource {
  [ExportGroup("Weapon")]
  [Export] public WeaponType type = WeaponType.Revolver;
  [Export] public float range = 50.0f;
  [Export] public float fireRate = 1.0f;
  [Export] public float reloadTime = 1.5f;
  [Export] public int magazineSize = 1;
  [Export] public float damageMulitplier = 1.0f;

  [ExportGroup("Projectile")]
  [Export] public PackedScene projectile;
  [Export] public float projectileSpeed = 50.0f;
  [Export] public float projectileDamage = 20.0f;
  [Export] public float projectileRange = 50.0f;

  [Export] public int projectileCount = 1;
  [Export] public float projectileSpread = 0.0f;

  [Signal] public delegate void EmptyEventHandler();
}

