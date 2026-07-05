using Godot;

[GlobalClass]
public partial class WeaponComponent : Node {
  // =========================
  // Weapon Selection
  // =========================

  [Export]
  public WeaponType CurrentWeapon = WeaponType.Shotgun;

  // =========================
  // Shotgun
  // =========================

  [ExportGroup("Shotgun")]

  [Export] public float ShotgunDamage = 12f;
  [Export] public int ShotgunPellets = 8;
  [Export] public float ShotgunSpread = 0.18f;
  [Export] public float ShotgunFireRate = 1f;
  [Export] public int ShotgunMagazine = 2;
  [Export] public float ShotgunReloadTime = 2.0f;

  // =========================
  // Crossbow
  // =========================

  [ExportGroup("Crossbow")]

  [Export] public float CrossbowDamage = 90f;
  [Export] public float CrossbowFireRate = 0.7f;
  [Export] public int CrossbowMagazine = 1;
  [Export] public float CrossbowReloadTime = 1.5f;

  // =========================
  // Runtime
  // =========================

  private int currentAmmo;
  private bool reloading = false;

  private double fireCooldown = 0;
  private double reloadTimer = 0;

  // =========================

  public override void _Ready() {
    EquipWeapon(CurrentWeapon);
  }

  public override void _PhysicsProcess(double delta) {
    if(fireCooldown > 0)
      fireCooldown -= delta;

    if(reloading) {
      reloadTimer -= delta;

      if(reloadTimer <= 0) {
        FinishReload();
      }
    }
  }

  // =========================
  // Shoot
  // =========================

  public void Shoot() {
    if(reloading)
      return;

    if(fireCooldown > 0)
      return;

    if(currentAmmo <= 0) {
      Reload();
      return;
    }

    currentAmmo--;

    switch(CurrentWeapon) {
      case WeaponType.Shotgun:

        fireCooldown = 1.0 / ShotgunFireRate;

        ShootShotgun();

        break;

      case WeaponType.Crossbow:

        fireCooldown = 1.0 / CrossbowFireRate;

        ShootCrossbow();

        break;
    }

    GD.Print("Ammo: " + currentAmmo);
  }

  // =========================

  private void ShootShotgun() {
    GD.Print("====================");
    GD.Print("SHOTGUN FIRED");
    GD.Print("====================");

    for(int i = 0; i < ShotgunPellets; i++) {
      float spreadX = (GD.Randf() - 0.5f) * ShotgunSpread;
      float spreadY = (GD.Randf() - 0.5f) * ShotgunSpread;

      GD.Print($"Pellet {i + 1}  Spread({spreadX:F2}, {spreadY:F2})");
    }

    GD.Print($"Damage per Pellet: {ShotgunDamage}");
  }

  // =========================

  private void ShootCrossbow() {
    GD.Print("====================");
    GD.Print("CROSSBOW FIRED");
    GD.Print("====================");

    GD.Print($"Damage: {CrossbowDamage}");
  }

  // =========================

  public void Reload() {
    if(reloading)
      return;

    reloading = true;

    switch(CurrentWeapon) {
      case WeaponType.Shotgun:

        reloadTimer = ShotgunReloadTime;

        break;

      case WeaponType.Crossbow:

        reloadTimer = CrossbowReloadTime;

        break;
    }

    GD.Print("Reloading...");
  }

  // =========================

  private void FinishReload() {
    reloading = false;

    switch(CurrentWeapon) {
      case WeaponType.Shotgun:

        currentAmmo = ShotgunMagazine;

        break;

      case WeaponType.Crossbow:

        currentAmmo = CrossbowMagazine;

        break;
    }

    GD.Print("Reload Finished");
    GD.Print("Ammo: " + currentAmmo);
  }

  // =========================
  // Weapon Switch
  // =========================

  public void EquipWeapon(WeaponType weapon) {
    CurrentWeapon = weapon;

    switch(weapon) {
      case WeaponType.Shotgun:

        currentAmmo = ShotgunMagazine;

        GD.Print("Equipped Shotgun");

        break;

      case WeaponType.Crossbow:

        currentAmmo = CrossbowMagazine;

        GD.Print("Equipped Crossbow");

        break;
    }
  }

  // =========================

  public int GetAmmo() {
    return currentAmmo;
  }

  public bool IsReloading() {
    return reloading;
  }
}
