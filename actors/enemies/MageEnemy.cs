using Godot;

public partial class MageEnemy : Enemy, IHitable {
  private MageEnemyStateMachine stateMachine;
  private RangedAttackComponent rangedAttack;
  private HurtboxComponent hurtboxComponent;
  private ProgressBar healthBar;

  private bool dead;

  [Export] private ShaderMaterial dissolveMaterial;
  [Export] private Node3D projectileSpawnPoint;

  // Animation state - use local variables, don't modify Position directly
  private float idleBreatheTime;
  private float idleFloatTime;
  private float bodySwayTime;

  // Store original position for animation offsets
  private float originalY;

  public override void _Ready() {
    base._Ready();

    stateMachine = GetComponent<MageEnemyStateMachine>();
    rangedAttack = GetComponent<RangedAttackComponent>();
    hurtboxComponent = GetComponent<HurtboxComponent>();

    healthComponent.HealthChanged += OnHealthChanged;
    healthComponent.Died += OnDeath;

    projectileSpawnPoint ??= GetNodeOrNull<Node3D>("ProjectileSpawn");

    originalY = Position.Y;

    // Health bar
    healthBar = GetNodeOrNull<ProgressBar>("SubViewport/HealthBar");
    if (healthBar != null) {
      healthBar.MaxValue = healthComponent.maxHealth;
      healthBar.Value = healthComponent.CurrentHealth;
    }

    // Connect to ranged attack signals
    if (rangedAttack != null) {
      rangedAttack.ChargeStarted += OnChargeStarted;
      rangedAttack.ProjectileFired += OnProjectileFired;
    }

    // Disable hitbox - mage doesn't melee
    var hitbox = GetComponent<HitboxComponent>();
    if (hitbox != null) {
      hitbox.DisableCollisionShapes();
    }
  }

  public override void _PhysicsProcess(double delta) {
    if (dead) { return; }

    input = behaviorTree.GetInput;
    behaviorTree.UpdateInfo(aiInfo);
    stateMachine.UpdateInput(input);

    Vector3 direction = new(input.direction.X, 0.0f, input.direction.Y);
    Direction = direction;

    if (!IsOnFloor()) {
      velocityComponent.AddVelocityInDirection(GetGravity() * (float)delta);
    }
    velocityComponent.Move(this);

    // Update procedural animations
    UpdateProceduralAnimations(delta);
  }

  private void UpdateProceduralAnimations(double delta) {
    string currentState = stateMachine.GetCurrentStateName();

    switch (currentState) {
      case "idle":
        UpdateIdleAnimation(delta);
        break;
      case "walk":
      case "backpedal":
        UpdateWalkAnimation(delta);
        break;
      case "attack_charge":
        UpdateChargeAnimation(delta);
        break;
      case "attack_cooldown":
        UpdateCooldownAnimation(delta);
        break;
    }
  }

  private void UpdateIdleAnimation(double delta) {
    idleBreatheTime += (float)delta;
    idleFloatTime += (float)delta * 0.5f;
    bodySwayTime += (float)delta * 0.3f;

    // Only apply small offsets - don't modify Position directly with Position = new Vector3
    // Use translation instead
    float floatOffset = Mathf.Sin(idleFloatTime) * 0.05f;
    float sway = Mathf.Sin(bodySwayTime) * 0.02f;

    // Store anim offset but don't override physics position
    Velocity = new Vector3(Velocity.X, Velocity.Y + floatOffset * (float)delta * 2.0f, Velocity.Z);
    Rotation = new Vector3(0.0f, Rotation.Y, sway);
  }

  private void UpdateWalkAnimation(double delta) {
    bodySwayTime += (float)delta * 2.0f;

    // Walking bob via small velocity offset
    float bob = Mathf.Abs(Mathf.Sin(bodySwayTime)) * 0.03f;
    float sway = Mathf.Sin(bodySwayTime * 0.5f) * 0.03f;
    Rotation = new Vector3(0.0f, Rotation.Y, sway);
  }

  private void UpdateChargeAnimation(double delta) {
    idleBreatheTime += (float)delta * 1.5f;

    float leanBack = Mathf.Lerp(0.0f, 0.1f, rangedAttack?.GetChargeProgress ?? 0.0f);
    Rotation = new Vector3(leanBack, Rotation.Y, 0.0f);
  }

  private void UpdateCooldownAnimation(double delta) {
    Rotation = new Vector3(
      Mathf.Lerp(Rotation.X, 0.0f, (float)delta * 3.0f),
      Rotation.Y,
      Mathf.Lerp(Rotation.Z, 0.0f, (float)delta * 3.0f)
    );
  }

  private void OnChargeStarted() {
    GD.Print("Mage: Charge started!");
  }

  private void OnProjectileFired() {
    GD.Print("Mage: Projectile fired!");
  }

  public void RecieveHit(HitInfo hitInfo) {
    healthComponent.TakeDamage(hitInfo.damage);

    if (!healthComponent.IsAlive && hitInfo.shooter is Player player) {
      player.GetComponent<InsanityComponent>().AddInsanity(10.0f);
    }

    if (healthComponent.IsAlive) {
      Tween tween = CreateTween();
      tween.TweenProperty(this, "rotation:x", 0.05f, 0.1f);
      tween.TweenProperty(this, "rotation:x", 0.0f, 0.2f);
    }
  }

  private void OnHealthChanged(float newHealth) {
    if (healthBar != null) {
      healthBar.Value = healthComponent.CurrentHealth;
    }
  }

  private async void OnDeath() {
    dead = true;

    // Freeze movement
    velocityComponent.Stop();

    // Find mesh in GLB
    MeshInstance3D? mesh = FindMeshInChildren(this);

    if (mesh != null && dissolveMaterial != null) {
      mesh.MaterialOverride = dissolveMaterial;
      if (mesh.MaterialOverride is ShaderMaterial meshShader) {
        meshShader.SetShaderParameter("t", 0.0);
        meshShader.SetShaderParameter("noise_scale", 1.0);

        Tween tween = CreateTween();
        tween.TweenMethod(
          Callable.From((float value) => meshShader.SetShaderParameter("t", value)),
          0.0, 1.0, 2.0
        );
        await ToSignal(tween, Tween.SignalName.Finished);
      }
    }

    QueueFree();
  }

  private MeshInstance3D? FindMeshInChildren(Node parent) {
    foreach (Node child in parent.GetChildren()) {
      if (child is MeshInstance3D mi && mi.Mesh != null) {
        return mi;
      }
      var found = FindMeshInChildren(child);
      if (found != null) { return found; }
    }
    return null;
  }
}