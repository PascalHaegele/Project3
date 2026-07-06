using Godot;

public partial class TestEnemy : Actor {
  private AIComponent aiComponent;
  private StateMachine stateMachine;
  private VelocityComponent velocityComponent;
  private HealthComponent healthComponent;

  private InputPackage input = new();

  public override void _Ready() {
    aiComponent = GetComponent<AIComponent>();

    stateMachine = GetComponent<StateMachine>();

    velocityComponent = GetComponent<VelocityComponent>();

    healthComponent = GetComponent<HealthComponent>();
    healthComponent.Died += OnDeath;
  }

  public override void _Process(double delta) {
    input = aiComponent.GetInput;
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

  private void OnDeath() {
    QueueFree();
  }
}

