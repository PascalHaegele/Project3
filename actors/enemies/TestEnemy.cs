using Godot;

public partial class TestEnemy : Enemy, IHitable {
  private TestEnemyStateMachine stateMachine;
  private HitboxComponent hitboxComponent;
  private HurtboxComponent hurtboxComponent;

  private ProgressBar healthBar;

  public override void _Ready() {
    base._Ready();

    stateMachine = GetComponent<TestEnemyStateMachine>();

    healthComponent.HealthChanged += OnHealthChanged;
    healthComponent.Died += OnDeath;

    hitboxComponent = GetComponent<HitboxComponent>();
    hitboxComponent.damage = 10.0f;
    hitboxComponent.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HITBOX;
    hitboxComponent.CollisionMask = (uint)CollisionLayerEnum.PLAYER_HURTBOX;

    hurtboxComponent = GetComponent<HurtboxComponent>();
    hurtboxComponent.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HURTBOX;
    hurtboxComponent.CollisionMask = (uint)CollisionLayerEnum.NONE;

    healthBar = GetComponent<ProgressBar>();
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;

    // detectionComponent.SeeingPlayer += OnSeeingPlayer;
    // detectionComponent.SoundHeard += OnSoundHeared;
  }

  public override void _Process(double delta) {
    input = aiStateMachine.GetInput;
    stateMachine.UpdateInput(input);
  }

  public override void _PhysicsProcess(double delta) {
    Vector3 direction = new(input.direction.X, 0.0f, input.direction.Y);
    Direction = direction;

    if(!IsOnFloor()) {
      velocityComponent.AddVelocityInDirection(GetGravity() * (float)delta);
    }
    velocityComponent.Move(this);
  }

  public void RecieveHit(HitInfo hitInfo) {
    healthComponent.TakeDamage(hitInfo.damage);
    // if(hitInfo.direction != Vector3.Zero) {
    //   aiStateMachine
    //     .OnStateTransition(aiStateMachine.GetState<AIStateSearch>());
    //   LookAt(GlobalPosition + hitInfo.direction);
    // }
  }

  private void OnSeeingPlayer(Vector3 position) {
    GD.Print($"{Name} sees player");
    playerInVision = true;
  }

  private void OnSoundHeared(Vector3 position) {
    GD.Print($"{Name} heard sound");
    if(!position.IsEqualApprox(GlobalPosition)) { LookAt(position); }
    hearingPlayer = true;
  }

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = healthComponent.CurrentHealth;
  }

  private void OnDeath() {
    QueueFree();
  }
}

