using Godot;

[GlobalClass]
public partial class Weapon : Node3D {
  [Export] public WeaponInfo info;
  [Export] private Marker3D projectileSpawn;

  private float fireCooldown;
  private float reloadTimer;

  private Projectile p;

  private RayCast3D aimCast;
  private RayCast3D projectileCast;

  private Actor actor;
  private InventoryComponent inventoryComponent;

  public ItemType AmmoType { get; private set; }

  public int CurrentAmmo { get; private set; }
  public bool Reloading { get; private set; }

  [Signal] public delegate void ShotEventHandler();
  [Signal] public delegate void ReloadedEventHandler();

  // --- Animation (delegiert an WeaponAnimation-Child) ---
  private WeaponAnimation weaponAnim;

  // Track shots for FrenziedSoul
  private int shotCounter;

  public override void _Ready() {
    aimCast = GetNode<RayCast3D>("../AimCast");
    aimCast.TargetPosition = new(0.0f, 0.0f, -info.range);

    projectileCast = GetNode<RayCast3D>("../ProjectileCast");

    actor = GetParent() as Actor ?? GetParent().GetParent<Actor>();
    inventoryComponent = actor.GetComponent<InventoryComponent>();

    AmmoType =
      info.type == WeaponType.Revolver ? ItemType.RAmmo : ItemType.SAmmo;

    CurrentAmmo = info.magazineSize;

    weaponAnim = GetNodeOrNull<WeaponAnimation>("WeaponAnimation");
  }

  public override void _PhysicsProcess(double delta) {
    if(fireCooldown > 0.0f) { fireCooldown -= (float)delta; }
    if(Reloading) {
      reloadTimer -= (float)delta;
      if(reloadTimer <= 0.0f) { FinishReload(); }
    }
  }

  public void Reset() {
    CurrentAmmo = info.magazineSize;
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

    // Track shot count for FrenziedSoul effect
    if(actor is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("FrenziedSoul")) {
        shotCounter++;
        float empoweredThreshold = socket.GetModifier("FrenziedSoul");
        if(shotCounter >= empoweredThreshold) {
          shotCounter = 0;
          p.isEmpowered = true;
          GD.Print($">>> FRENZIED SOUL! Every 10th shot empowered.");
        }
      }
    }

    if(aimCast.IsColliding()) {
      Vector3 collisionPoint = aimCast.GetCollisionPoint();

      if(projectileCast.IsColliding()) {
        float distance =
          projectileSpawn.GlobalPosition.DistanceTo(collisionPoint);

        Vector3 position = projectileSpawn.GlobalPosition;
        projectileSpawn.Position += new Vector3(0.0f, 0.0f, distance + 0.05f);
        p.GlobalPosition = projectileSpawn.GlobalPosition;
        projectileSpawn.GlobalPosition = position;

        p.GlobalRotation = projectileSpawn.GlobalRotation;
      } else {
        p.GlobalPosition = projectileSpawn.GlobalPosition;
        p.LookAt(collisionPoint);
      }
    } else {
      p.GlobalPosition = projectileSpawn.GlobalPosition;
      p.GlobalRotation = projectileSpawn.GlobalRotation;
    }
    p.shotPosition = p.GlobalPosition;
    p.TopLevel = true;

    weaponAnim?.PlayRecoil();

    EmitSignalShot();

    GD.Print($"{Name} fired");
    GD.Print($"{Name} Ammo: {CurrentAmmo}");
  }

  public void PlayJumpAnim() { weaponAnim?.PlayJump(); }
  public void PlayDashAnim() { weaponAnim?.PlayDash(); }
  public void PlaySprintAnim(bool active) { weaponAnim?.PlaySprint(active); }
  public void PlayReloadAnim() { weaponAnim?.PlayReload(); }

  public void Reload() {
    if(
      Reloading ||
      CurrentAmmo >= info.magazineSize ||
      inventoryComponent.AmountOf(AmmoType) <= 0
    ) { return; }

    Reloading = true;
    float reloadDuration = info.reloadTime;

    if(actor is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null) {
        float reloadBonus = socket.GetModifier("ReloadSpeed");
        reloadDuration -= reloadBonus;
        reloadDuration = Mathf.Max(0.1f, reloadDuration);
        GD.Print(
          $"ReloadSpeed modifier: -{reloadBonus}, " +
          $"new duration: {reloadDuration:F2}"
        );
      }
    }
    reloadTimer = reloadDuration;

    weaponAnim?.PlayReload();

    GD.Print($"{Name} Reloading (duration: {reloadDuration:F2})");
  }

  public void FinishReload() {
    Reloading = false;

    CurrentAmmo +=
      inventoryComponent
        .RemoveItem(AmmoType, info.magazineSize - CurrentAmmo);

    EmitSignalReloaded();

    GD.Print($"{Name} reload finished");
    GD.Print($"{Name} Ammo: {CurrentAmmo}");
  }
}
