using Godot;

public partial class TestEnemy : Enemy {
  private HitboxComponent hitboxComponent;
  private AnimationPlayer animationPlayer;

  public override void _Ready() {
    base._Ready();

    hitboxComponent = GetComponent<HitboxComponent>();
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

    if(patrolPath != null) { GlobalPosition = patrolPath[0].GlobalPosition; }

    healthComponent.Died += OnDeath;

    if(visionArea != null) {
      visionArea.BodyEntered += OnPlayerEnteredVision;
      visionArea.BodyExited += OnPlayerExitedVision;
    }

    // aiComponent.Attacking += OnAttacking;
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

  private void OnPlayerEnteredVision(Node3D body) {
    playerInVision = true;
    GD.Print($"Player entered Vision of {Name}");
    // GD.Print($"{Name} chasing");
    // aiComponent.currentState = AIStateEnum.Chase;
  }

  private void OnPlayerExitedVision(Node3D body) {
    playerInVision = false;
    GD.Print($"Player exited Vision of {Name}");
    // GD.Print($"{Name} searching");
    // aiComponent.navAgent.TargetPosition = GlobalPosition;
    // aiComponent.currentState = AIStateEnum.Search;
  }

  private void OnAttacking() {
    animationPlayer.Play("attack");
  }

  private void OnDeath() {
    QueueFree();
  }
}

