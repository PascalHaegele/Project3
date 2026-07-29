using Godot;

public partial class TestEnemy : Enemy, IHitable {
  private BehaviorTree behaviorTree;
  private TestEnemyStateMachine stateMachine;
  private AIDetectionComponent detectionComponent;

  private HitboxComponent hitboxComponent;
  private ProgressBar healthBar;
    private bool dead;

  public override void _Ready() {
    base._Ready();

    behaviorTree = GetComponent<BehaviorTree>();
    stateMachine = GetComponent<TestEnemyStateMachine>();
    detectionComponent = GetComponent<AIDetectionComponent>();

    healthComponent.HealthChanged += OnHealthChanged;
    healthComponent.Died += OnDeath;

    hitboxComponent = GetComponent<HitboxComponent>();
    hitboxComponent.damage = 10.0f;
    hitboxComponent.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HITBOX;
    hitboxComponent.CollisionMask = (uint)CollisionLayerEnum.PLAYER_HURTBOX;
    hitboxComponent.DisableCollisionShapes();

    healthBar = GetComponent<ProgressBar>();
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;

    ApplyDifficulty();
    healthComponent.Reset();
  }

  public override void _PhysicsProcess(double delta) {
    if(dead) { return; }
    if(!healthComponent.IsAlive) { return; }

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

    Vector3 direction = hitInfo.direction;
    direction.Y = 0.0f;
    aiInfo.shotFromDirection = direction;
    aiInfo.beeingShot = true;
  }

  protected override void ApplyDifficulty() {
    velocityInfo.multiplier = difficultyInfo.speedMultiplier;
    hitboxComponent.damageMultiplier = difficultyInfo.damageMultiplier;
    healthComponent.multiplier = difficultyInfo.healthMultiplier;
  }

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = healthComponent.CurrentHealth;
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