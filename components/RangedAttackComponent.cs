using Godot;

[GlobalClass]
public partial class RangedAttackComponent : Node {
  [Export] public PackedScene projectileScene;
  [Export] public Node3D projectileSpawnPoint;
  [Export] public float projectileSpeed = 20.0f;
  [Export] public float projectileDamage = 15.0f;
  [Export] public float chargeDuration = 0.9f;
  [Export] public float cooldownDuration = 2.5f;
  [Export] public float projectileLifetime = 5.0f;
  [Export] public float preferredRangeMin = 8.0f;
  [Export] public float preferredRangeMax = 14.0f;

  private float cooldownTimer;
  private float chargeTimer;
  private bool isCharging;
  private bool isOnCooldown;
  private Enemy enemy;

  [Signal] public delegate void ChargeStartedEventHandler();
  [Signal] public delegate void ChargeProgressEventHandler(float progress);
  [Signal] public delegate void ProjectileFiredEventHandler();
  [Signal] public delegate void CooldownStartedEventHandler();
  [Signal] public delegate void CooldownEndedEventHandler();

  public bool IsCharging => isCharging;
  public bool IsOnCooldown => isOnCooldown;
  public float GetChargeProgress => isCharging ? Mathf.Clamp(chargeTimer / chargeDuration, 0.0f, 1.0f) : 0.0f;
  public float GetCooldownProgress => isOnCooldown ? Mathf.Clamp(cooldownTimer / cooldownDuration, 0.0f, 1.0f) : 0.0f;

  public override void _Ready() {
    enemy = GetParent<Enemy>();

    if (projectileSpawnPoint == null) {
      projectileSpawnPoint = GetNodeOrNull<Node3D>("ProjectileSpawn");
    }

    if (projectileScene == null) {
      projectileScene = GD.Load<PackedScene>("res://actors/enemies/mage_projectile.tscn");
      if (projectileScene == null) {
        GD.Print("RangedAttackComponent: _Ready fallback load FAILED");
      }
    }
  }

  public override void _PhysicsProcess(double delta) {
    if (isCharging) {
      chargeTimer += (float)delta;
      EmitSignalChargeProgress(GetChargeProgress);
      if (chargeTimer >= chargeDuration) {
        FireProjectile();
      }
    }

    if (isOnCooldown) {
      cooldownTimer -= (float)delta;
      if (cooldownTimer <= 0.0f) {
        isOnCooldown = false;
        EmitSignalCooldownEnded();
      }
    }
  }

  public void StartCharge() {
    if (isOnCooldown || isCharging) return;
    isCharging = true;
    chargeTimer = 0.0f;
    EmitSignalChargeStarted();
  }

  public void CancelCharge() {
    isCharging = false;
    chargeTimer = 0.0f;
  }

  public bool IsInPreferredRange(float distance) {
    return distance >= preferredRangeMin && distance <= preferredRangeMax;
  }

  public bool IsTooClose(float distance) {
    return distance < preferredRangeMin;
  }

  public bool IsTooFar(float distance) {
    return distance > preferredRangeMax;
  }

  private void FireProjectile() {
    isCharging = false;
    chargeTimer = 0.0f;

    Node3D spawnPoint = projectileSpawnPoint ?? enemy;
    Vector3 spawnPos = spawnPoint.GlobalPosition;

    MageProjectile projectile = CreateProjectileInCode();
    if (projectile == null) {
      GD.PrintErr("RangedAttackComponent: Failed to create projectile!");
      StartCooldown();
      return;
    }

    enemy.GetParent().AddChild(projectile);
    projectile.GlobalPosition = spawnPos;
    projectile.TopLevel = true;

    projectile.speed = projectileSpeed;
    projectile.damage = projectileDamage;
    if (projectile.hitbox != null) {
      projectile.hitbox.damage = projectileDamage;
      projectile.hitbox.actor = enemy;
    }

    // Ensure the projectile has a valid target
    Node3D playerTarget = GetTree().GetFirstNodeInGroup("player") as Node3D;
    if (playerTarget != null) {
      projectile.target = playerTarget;
      projectile.currentDirection = (playerTarget.GlobalPosition - spawnPos).Normalized();
      projectile.LookAt(playerTarget.GlobalPosition + new Vector3(0, 1, 0), Vector3.Up);
      GD.Print("RangedAttackComponent: Target set to ", playerTarget.Name, " dir=", projectile.currentDirection);
    } else {
      // Fallback: fire forward from spawn point
      projectile.currentDirection = -enemy.GlobalTransform.Basis.Z.Normalized();
      GD.Print("RangedAttackComponent: No player target found, firing forward");
    }

    GD.Print("RangedAttackComponent: Projectile fired! name=", projectile.Name,
             " parent=", projectile.GetParent()?.Name,
             " pos=", projectile.GlobalPosition,
             " speed=", projectile.speed);
    EmitSignalProjectileFired();
    StartCooldown();
  }

  private MageProjectile CreateProjectileInCode() {
    MageProjectile projectile = new MageProjectile();
    projectile.Name = "MageProjectile";

    StandardMaterial3D mat = new StandardMaterial3D();
    mat.AlbedoColor = new Color(0.2f, 0.6f, 1.0f);
    mat.Emission = new Color(0.2f, 0.6f, 1.0f);
    mat.EmissionEnergyMultiplier = 2.0f;
    mat.Metallic = 0.5f;
    mat.Roughness = 0.3f;

    SphereMesh mesh = new SphereMesh();
    mesh.Radius = 0.12f;
    mesh.Height = 0.24f;
    mesh.Material = mat;
    MeshInstance3D meshInstance = new MeshInstance3D();
    meshInstance.Mesh = mesh;
    projectile.AddChild(meshInstance);

    OmniLight3D light = new OmniLight3D();
    light.LightColor = new Color(0.2f, 0.6f, 1.0f);
    light.LightEnergy = 0.5f;
    light.OmniRange = 2.0f;
    projectile.AddChild(light);

    SphereShape3D shape = new SphereShape3D();
    shape.Radius = 0.15f;
    CollisionShape3D collisionShape = new CollisionShape3D();
    collisionShape.Shape = shape;
    projectile.AddChild(collisionShape);

    HitboxComponent hitbox = new HitboxComponent();
    hitbox.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HITBOX;
    hitbox.CollisionMask = (uint)CollisionLayerEnum.PLAYER_HURTBOX;
    hitbox.Monitoring = true;
    hitbox.Monitorable = true;
    projectile.AddChild(hitbox);
    projectile.hitbox = hitbox;

    projectile.CollisionLayer = 0;
    projectile.CollisionMask = (uint)(CollisionLayerEnum.WORLD | CollisionLayerEnum.PLAYER);

    return projectile;
  }

  private void StartCooldown() {
    isOnCooldown = true;
    cooldownTimer = cooldownDuration;
    EmitSignalCooldownStarted();
  }
}