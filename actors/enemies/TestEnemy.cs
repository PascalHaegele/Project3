using Godot;

public partial class TestEnemy : Enemy, IHitable {
  private TestEnemyStateMachine stateMachine;
  private HitboxComponent hitboxComponent;
  private HurtboxComponent hurtboxComponent;

  private ProgressBar healthBar;

  private bool dead;

  [Export] private ShaderMaterial dissolveMaterial;

  public override void _Ready() {
    base._Ready();

    stateMachine = GetComponent<TestEnemyStateMachine>();

    healthComponent.HealthChanged += OnHealthChanged;
    healthComponent.Died += OnDeath;

    hitboxComponent = GetComponent<HitboxComponent>();
    hitboxComponent.damage = 10.0f;
    hitboxComponent.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HITBOX;
    hitboxComponent.CollisionMask = (uint)CollisionLayerEnum.PLAYER_HURTBOX;
    hitboxComponent.DisableCollisionShapes();

    hurtboxComponent = GetComponent<HurtboxComponent>();
    hurtboxComponent.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HURTBOX;
    hurtboxComponent.CollisionMask = (uint)CollisionLayerEnum.NONE;


    healthBar = GetComponent<ProgressBar>();
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;
  }

  public override void _PhysicsProcess(double delta) {
    if(dead) { return; }

    input = behaviorTree.GetInput;
    behaviorTree.UpdateInfo(aiInfo);
    stateMachine.UpdateInput(input);

    Vector3 direction = new(input.direction.X, 0.0f, input.direction.Y);
    Direction = direction;

    if(!IsOnFloor()) {
      velocityComponent.AddVelocityInDirection(GetGravity() * (float)delta);
    }
    velocityComponent.Move(this);
  }

  public void RecieveHit(HitInfo hitInfo) {
    healthComponent.TakeDamage(hitInfo.damage);
    if(hitInfo.direction != Vector3.Zero && !playerInVision) {
      Vector3 direction = hitInfo.direction;
      direction.Y = GlobalPosition.Y;
      LookAt(GlobalPosition + direction);
    }
  }

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = healthComponent.CurrentHealth;
  }

  private async void OnDeath() {
    // SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
    dead = true;

    if(GetTree().Root.FindChild("Player", true, false) is Player player) {
      player.GetComponent<HealthComponent>()?.OnEliteKill();
      GD.Print($">>> DEBUG BloodRitual trigger: enemy defeated, player health={player.GetComponent<HealthComponent>()?.CurrentHealth:F1}");
    }

    if(dissolveMaterial != null) {
      MeshInstance3D mesh = GetNode<MeshInstance3D>("MeshInstance3D");
      mesh.MaterialOverride = dissolveMaterial;
      ShaderMaterial meshShader = mesh.MaterialOverride as ShaderMaterial;
      meshShader.SetShaderParameter("t", 0.0);

      Tween tween = CreateTween();
      _ = tween.TweenMethod(
        Callable.From(
          (float value) => meshShader.SetShaderParameter("t", value)
        ),
        0.0,
        1.0,
        3.0
      );

      _ = await ToSignal(tween, Tween.SignalName.Finished);
    }

    QueueFree();
  }
}

