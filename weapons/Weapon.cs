using Godot;

[GlobalClass]
public partial class Weapon : Node3D {
  [Export] public WeaponInfo info;
  [Export] private Marker3D projectileSpawn;

  private float fireCooldown;

  private Projectile p;

  private RayCast3D aimCast;
  private RayCast3D projectileCast;

  public Actor actor;
  private InventoryComponent inventoryComponent;

  public ItemType AmmoType { get; private set; }

  public int CurrentAmmo { get; private set; }
  public bool Reloading { get; private set; }

  [Signal] public delegate void ShotEventHandler();
  [Signal] public delegate void ReloadedEventHandler();

  // --- Animation (delegiert an WeaponAnimation-Child) ---
  public WeaponAnimation weaponAnim;
  private bool waitingForReloadAnimation;

  // Track shots for FrenziedSoul
  private bool empowerNextShot = false;

  public override void _Ready() {
    aimCast = GetNode<RayCast3D>("../AimCast");
    aimCast.TargetPosition = new(0.0f, 0.0f, -info.range);

    projectileCast = GetNode<RayCast3D>("../ProjectileCast");

    actor = GetParent() as Actor ?? GetParent().GetParent<Actor>();
    inventoryComponent = actor.GetComponent<InventoryComponent>();

    AmmoType =
      info.type == WeaponType.Revolver ? ItemType.R_AMMO : ItemType.S_AMMO;

    CurrentAmmo = info.magazineSize;

    weaponAnim = GetNodeOrNull<WeaponAnimation>("WeaponAnimation");
    if(weaponAnim != null) {
      weaponAnim.ReloadVisualComplete += OnReloadVisualComplete;
      weaponAnim.CameraShake += OnWeaponCameraShake;
      weaponAnim.CameraRecoil += OnWeaponCameraRecoil;
      weaponAnim.MuzzleFlash += OnMuzzleFlash;
    }
  }

  public override void _ExitTree() {
    if(weaponAnim != null) {
      weaponAnim.ReloadVisualComplete -= OnReloadVisualComplete;
      weaponAnim.CameraShake -= OnWeaponCameraShake;
      weaponAnim.CameraRecoil -= OnWeaponCameraRecoil;
      weaponAnim.MuzzleFlash -= OnMuzzleFlash;
    }
  }

  public override void _PhysicsProcess(double delta) {
    if(fireCooldown > 0.0f) { fireCooldown -= (float)delta; }
  }

  public void Reset() {
    CurrentAmmo = info.magazineSize;
    empowerNextShot = false;
  }

  public void Shoot() {
    if(Reloading || fireCooldown > 0.0f) { return; }
    if(CurrentAmmo <= 0) { Reload(); return; }

    CurrentAmmo--;

    fireCooldown = 1.0f / info.fireRate;

    p = info.projectile.Instantiate<Projectile>();
    if(p == null) { return; }

    // Track shot count for FrenziedSoul effect
    if(actor is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("FrenziedSoul")) {
        if(empowerNextShot) {
          p.isEmpowered = true;
          GD.Print($">>> DEBUG FrenziedSoul: EMPOWERED SHOT after reload!");
          empowerNextShot = false;
        }
      }
    }

    AddChild(p);
    p.RecalculateDamage();

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

  public void Reload() {
    if(
      Reloading ||
      waitingForReloadAnimation ||
      CurrentAmmo >= info.magazineSize ||
      inventoryComponent.AmountOf(AmmoType) <= 0
    ) { return; }

    Reloading = true;
    waitingForReloadAnimation = true;

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

    weaponAnim?.PlayReload(reloadDuration);

    GD.Print($"{Name} Reloading (duration: {reloadDuration:F2})");
  }

  private void OnReloadVisualComplete() {
    waitingForReloadAnimation = false;
    Reloading = false;

    CurrentAmmo +=
      inventoryComponent
        .RemoveItem(AmmoType, info.magazineSize - CurrentAmmo);

    // Empower next shot after reload if FrenziedSoul is active
    if(actor is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("FrenziedSoul")) {
        empowerNextShot = true;
        GD.Print($">>> DEBUG FrenziedSoul: reload finished, next shot will be empowered");
      }
    }

    EmitSignalReloaded();

    GD.Print($"{Name} reload finished (magazine refilled)");
    GD.Print($"{Name} Ammo: {CurrentAmmo}");
  }

  private void OnWeaponCameraShake(float amount, float duration) {
    if(actor is Player player) {
      CameraComponent cam = player.GetComponent<CameraComponent>();
      cam?.Shake(amount, duration);
    }
  }

  private void OnWeaponCameraRecoil(float amount) {
    if(actor is Player player) {
      CameraComponent cam = player.GetComponent<CameraComponent>();
      cam?.RecoilKick(amount);
    }
  }

  private void OnMuzzleFlash(Vector3 globalPosition, Vector3 forward) {
    // PackedScene flashScene = GD.Load<PackedScene>("res://weapons/muzzle_flash/MuzzleFlashEffect.tscn");
    // if(flashScene == null) { return; }
    // Node3D flash = flashScene.Instantiate<Node3D>();
    // GetTree().Root.AddChild(flash);
    // // Use projectileSpawn position (barrel tip) instead of weapon handle
    // flash.GlobalPosition = projectileSpawn.GlobalPosition;
    // float yaw = Mathf.Atan2(forward.X, -forward.Z);
    // flash.GlobalRotation = new Vector3(0.0f, yaw, 0.0f);

    foreach(Node child in GetNode("MuzzleFlash2").GetChildren()) {
      if(child is GpuParticles3D particle) { particle.Emitting = true; }
    }
  }
}
