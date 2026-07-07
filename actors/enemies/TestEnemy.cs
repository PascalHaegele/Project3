using Godot;

public partial class TestEnemy : Enemy {
  public override void _Ready() {
    base._Ready();

    healthComponent.Died += OnDeath;

    visionArea.BodyEntered += OnPlayerEnteredVision;
    visionArea.BodyExited += OnPlayerExitedVision;
  }

  public override void _Process(double delta) {
    input = aiComponent.GetInput();
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

  private void OnPlayerEnteredVision(Node3D body) {
    GD.Print($"Player entered Vision of {Name}");
    aiComponent.currentState = AIState.Chase;
  }

  private void OnPlayerExitedVision(Node3D body) {
    GD.Print($"Player exited Vision of {Name}");
    aiComponent.currentState = AIState.Patrol;
    aiComponent.navAgent.TargetPosition = GlobalPosition;
  }

  private void OnDeath() {
    QueueFree();
  }
}

