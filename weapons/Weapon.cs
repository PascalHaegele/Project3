using Godot;

public enum WeaponType { Crossbow, Shotgun }

[GlobalClass]
public partial class Weapon : Node3D {
  [Export] private WeaponInfo info;
  [Export] private PackedScene projectile;
  private Marker3D? projectileSpawn;

  private float fireCooldown;
  private float reloadTimer;

  private Projectile? p;

  public int CurrentAmmo { get; private set; }
  public bool Reloading { get; private set; }

  public float DamageMultiplier => info.damageMulitplier;

  public override void _Ready() {
    projectileSpawn = GetNodeOrNull<Marker3D>("ProjectileSpawn");
    if(projectileSpawn == null) {
      GD.PrintErr($"{Name} missing Projectile Spawn Marker");
    }
    CurrentAmmo = info.magazineSize;

    SpawnProjectile();
  }

  public override void _PhysicsProcess(double delta) {
    if(fireCooldown > 0.0f) { fireCooldown -= (float)delta; }
    if(Reloading) {
      reloadTimer -= (float)delta;
      if(reloadTimer <= 0.0f) {
        FinishReload();
      }
    }
  }

  public void Shoot() {
    if(Reloading || fireCooldown > 0.0f) { return; }
    if(CurrentAmmo <= 0) { Reload(); return; }

    CurrentAmmo--;

    fireCooldown = 1.0f / info.fireRate;

    p.ProcessMode = ProcessModeEnum.Inherit;
    p.TopLevel = true;

    GD.Print($"{Name} fired");
    GD.Print($"{Name} Ammo: {CurrentAmmo}");
  }

  public void Reload() {
    if(Reloading || CurrentAmmo >= info.magazineSize) { return; }

    Reloading = true;
    reloadTimer = info.reloadTime;

    GD.Print($"{Name} Reloading");
  }

  public void FinishReload() {
    Reloading = false;
    CurrentAmmo = info.magazineSize;

    SpawnProjectile();

    GD.Print($"{Name} reload finished");
    GD.Print($"{Name} Ammo: {CurrentAmmo}");
  }

  private void SpawnProjectile() {
    p = projectile.InstantiateOrNull<Projectile>();
    if(p == null) { return; }
    AddChild(p);
    p.GlobalPosition =
      projectileSpawn != null ? projectileSpawn.GlobalPosition : GlobalPosition;
    p.GlobalRotation =
      projectileSpawn != null ? projectileSpawn.GlobalRotation : GlobalRotation;
    p.ProcessMode = ProcessModeEnum.Disabled;
  }
}

