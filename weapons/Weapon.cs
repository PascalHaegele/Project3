using Godot;

[GlobalClass]
public partial class Weapon : Node3D {
  [Export] public WeaponInfo info;
  [Export] protected Marker3D projectileSpawn;

  protected float fireCooldown;
  protected float reloadTimer;

  protected Projectile p;

  public int CurrentAmmo { get; protected set; }
  public bool Reloading { get; protected set; }

  public float DamageMultiplier => info.damageMulitplier;

  public override void _Ready() {
    if(projectileSpawn == null) {
      GD.PrintErr($"{Name} missing Projectile Spawn Marker");
    }

    CurrentAmmo = info.magazineSize;

    // SpawnProjectiles();
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

    p = info.projectile.Instantiate<Projectile>();
    // p.hitbox.EnableCollisionShapes();
    if(p == null) { return; }
    AddChild(p);
    p.GlobalPosition =
      projectileSpawn != null ? projectileSpawn.GlobalPosition : GlobalPosition;
    p.GlobalRotation =
      projectileSpawn != null ? projectileSpawn.GlobalRotation : GlobalRotation;
    p.ProcessMode = ProcessModeEnum.Inherit;
    p.shotPosition = GlobalPosition;
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

    // SpawnProjectiles();

    GD.Print($"{Name} reload finished");
    GD.Print($"{Name} Ammo: {CurrentAmmo}");
  }

  private void SpawnProjectiles() {
    p = info.projectile.InstantiateOrNull<Projectile>();
    if(p == null) { return; }
    AddChild(p);
    p.GlobalPosition =
      projectileSpawn != null ? projectileSpawn.GlobalPosition : GlobalPosition;
    p.GlobalRotation =
      projectileSpawn != null ? projectileSpawn.GlobalRotation : GlobalRotation;
    p.hitbox.DisableCollisionShapes();
    p.ProcessMode = ProcessModeEnum.Disabled;
  }
}

