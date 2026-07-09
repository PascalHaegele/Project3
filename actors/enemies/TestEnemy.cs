using Godot;

public partial class TestEnemy : Enemy {
  private HitboxComponent hitboxComponent;

  private ProgressBar healthBar;

  public override void _Ready() {
    base._Ready();

    healthComponent.HealthChanged += OnHealthChanged;
    healthComponent.Died += OnDeath;

    hitboxComponent = GetComponent<HitboxComponent>();
    hitboxComponent.damage = 10.0f;

    healthBar = GetComponent<ProgressBar>();
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;
  }

  public override void _Process(double delta) {
    input = aiStateMachine.GetInput;
    stateMachine.input = input;
  }

  public override void _PhysicsProcess(double delta) {
    Vector3 direction = new(input.direction.X, 0.0f, input.direction.Y);
    Direction = direction;

    if(!IsOnFloor()) {
      velocityComponent.AddVelocityInDirection(GetGravity() * (float)delta);
    }
    velocityComponent.Move(this);
  }

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = healthComponent.CurrentHealth;
  }

  private void OnDeath() {
    QueueFree();
  }
}

