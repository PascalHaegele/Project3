using Godot;
using System.Collections.Generic;

public partial class TestEnemy : Enemy, IHitable {
  private BehaviorTree behaviorTree;
  private TestEnemyStateMachine stateMachine;
  private AIDetectionComponent detectionComponent;

  private HitboxComponent hitboxComponent;
  private ProgressBar healthBar;
   [Export] public string attackSoundEvent = "event:/Knight_Attack_Action";
  // [Export] private Material dissolveMaterial;

  public override void _Ready() {
    base._Ready();

    behaviorTree = GetComponent<BehaviorTree>();
    stateMachine = GetComponent<TestEnemyStateMachine>();
    detectionComponent = GetComponent<AIDetectionComponent>();

    healthComponent.HealthChanged += OnHealthChanged;
    // healthComponent.Died += OnDeath;

    hitboxComponent = GetComponent<HitboxComponent>();
    hitboxComponent.actor = this;
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

    if(!healthComponent.IsAlive && hitInfo.shooter is Player) {
      EmitSignalKilled(this);
    }
  }

  protected override async void OnDeath() {
    base.OnDeath();

    if (behaviorTree != null) {
      behaviorTree.SetProcess(false);
      behaviorTree.SetPhysicsProcess(false);
    }

    if (stateMachine != null) {
      stateMachine.autoUpdate = false;
      stateMachine.SetPhysicsProcess(false);
    }

    if (detectionComponent != null) {
      detectionComponent.SetProcess(false);
      detectionComponent.SetPhysicsProcess(false);
    }

    SetProcess(false);
    SetPhysicsProcess(false);
  }

  protected override void ApplyDifficulty() {
    velocityInfo.multiplier = difficultyInfo.speedMultiplier;
    hitboxComponent.damageMultiplier = difficultyInfo.damageMultiplier;
    healthComponent.multiplier = difficultyInfo.healthMultiplier;
  }

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = healthComponent.CurrentHealth;
  }
}

