using FmodSharp;
using Godot;

public enum WeaponSoundType {
  Shot,
  Reload,
  EmptyShot,
}

[GlobalClass]
public partial class Weapon : Node3D {
  [Export] public WeaponInfo info;
  [Export] private Marker3D projectileSpawn;

  private float fireCooldown;

  private RayCast3D aimCast;
  private RayCast3D projectileCast;

  public Actor actor;
  private InventoryComponent inventoryComponent;

  // --- Animation (delegiert an WeaponAnimation-Child) ---
  public WeaponAnimation weaponAnim;
  private bool waitingForReloadAnimation;

  // Track shots for FrenziedSoul
  private bool empowerNextShot;

  [Export] private StringName shootEventPath;
  [Export] private StringName emptyShootEventPath;
  [Export] private StringName reloadEventPath;
  private FmodEvent reloadEvent;

  public ItemType AmmoType { get; private set; }

  public int CurrentAmmo { get; private set; }
  public bool Reloading { get; private set; }

  [Signal] public delegate void ShotEventHandler();
  [Signal] public delegate void ReloadedEventHandler();

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

    projectileSpawn ??= GetNode<Marker3D>("ProjectileSpawn");

    if(reloadEventPath != null) {
      reloadEvent = FmodServerWrapper.CreateEventInstance(reloadEventPath);
      AddChild(reloadEvent);
    }
  }

  public override void _PhysicsProcess(double delta) {
    if(fireCooldown > 0.0f) { fireCooldown -= (float)delta; }
  }

  public void Reset() {
    empowerNextShot = false;
    fireCooldown = 0.0f;
    Reloading = false;
    waitingForReloadAnimation = false;
  }

  public void Shoot() {
    if(Reloading || fireCooldown > 0.0f) { return; }
    if(CurrentAmmo <= 0) {
      // PlayWeaponSound(WeaponSoundType.EmptyShot);
      if(emptyShootEventPath != null) {
        FmodServerWrapper.PlayOneShotAttached(emptyShootEventPath, this);
      }
      return;
    }

    CurrentAmmo--;

    fireCooldown = 1.0f / info.fireRate;

    int pellets = Mathf.Max(1, info.projectileCount);
    for(int i = 0; i < pellets; i++) {
      Projectile p = info.projectile.Instantiate<Projectile>();
      if(p == null) { continue; }

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

      p.GlobalPosition = projectileSpawn.GlobalPosition;
      p.GlobalRotation = projectileSpawn.GlobalRotation;

      if(aimCast.IsColliding()) {
        Vector3 collisionPoint = aimCast.GetCollisionPoint();

        if(projectileCast.IsColliding()) {
          float distance =
            projectileCast.GlobalPosition.DistanceTo(collisionPoint);

          Vector3 position = projectileSpawn.GlobalPosition;
          projectileSpawn.Position += new Vector3(0.0f, 0.0f, distance + 0.05f);

          p.GlobalPosition = projectileSpawn.GlobalPosition;
          p.GlobalRotation = projectileSpawn.GlobalRotation;

          projectileSpawn.GlobalPosition = position;
        } else {
          p.GlobalPosition = projectileSpawn.GlobalPosition;
          p.LookAt(collisionPoint);
        }
      }

      float spread = info.projectileSpread;
      if(spread > 0.0f) {
        float yaw = (float)GD.RandRange(-spread, spread);
        float pitch = (float)GD.RandRange(-spread, spread);

        p.RotateX(pitch);
        p.RotateY(yaw);
      }

      p.shotPosition = p.GlobalPosition;
      p.TopLevel = true;
    }

    weaponAnim?.PlayRecoil();

    // PlayWeaponSound(WeaponSoundType.Shot);
    if(shootEventPath != null) {
      FmodServerWrapper.PlayOneShotAttached(shootEventPath, this);
    }

    EmitSignalShot();
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

    // PlayWeaponSound(WeaponSoundType.Reload);
    reloadEvent?.SetParameterByName("ShotCount", CurrentAmmo);
    reloadEvent?.Start();
  }

  public void OnReloadVisualComplete() {
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
    Node3D muzzleRoot = GetNodeOrNull<Node3D>("MuzzleFlash");
    if(muzzleRoot != null) {
      foreach(Node child in muzzleRoot.GetChildren()) {
        if(child is GpuParticles3D particle) { particle.Emitting = true; }
      }
    }
  }

  private string GetEventPathForType(WeaponSoundType soundType) {
    bool isRevolver = (info != null && info.type == WeaponType.Revolver);

    switch(soundType) {
      case WeaponSoundType.Shot:
        return
          isRevolver ?
          "event:/GunShot_Timeline" :
          "event:/ShotgunShot_Timeline";

      case WeaponSoundType.Reload:
        return
          isRevolver ?
          "event:/Gun_Reload_Timeline" :
          "event:/Shotgun_Reload_Timeline";

      case WeaponSoundType.EmptyShot:
        return "event:/EmptyWeapon_Action";

      default:
        return string.Empty;
    }
  }

  public void PlayWeaponSound(WeaponSoundType soundType) {
    string eventPath = GetEventPathForType(soundType);

    if(!string.IsNullOrEmpty(eventPath)) {
      var fmodServer = Engine.GetSingleton("FmodServer");
      if(fmodServer != null) {

        if(soundType == WeaponSoundType.Reload) {
          var eventInstance =
            fmodServer
              .Call("create_event_instance", eventPath)
              .As<GodotObject>();

          if(eventInstance != null && IsInstanceValid(eventInstance)) {
            _ = eventInstance
              .Call("set_parameter_by_name", "ShotCount", (float)CurrentAmmo);

            _ = eventInstance.Call("start");
            _ = eventInstance.Call("release");

            GD.Print(
              $">>> FMOD Reload gespielt ({info.type} | Ammo: {CurrentAmmo})"
            );
          }
        } else {

          _ = fmodServer.Call("play_one_shot", eventPath);
          GD.Print($">>> FMOD 2D-Sound gespielt: {soundType} ({eventPath})");
        }

      } else {
        GD.PrintErr(">>> FMOD Fehler: FmodServer-Singleton nicht gefunden!");
      }
    }
  }
}
