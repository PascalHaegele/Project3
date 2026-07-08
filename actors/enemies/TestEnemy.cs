using Godot;

public partial class TestEnemy : Enemy {
  public override void _Ready() {
    base._Ready();

    healthComponent.Died += OnDeath;
  }

  public override void _Process(double delta) {
    // input = aiComponent.GetInput();
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

  private void OnDeath() {
    QueueFree();
  }
}

