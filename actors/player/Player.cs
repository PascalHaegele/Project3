using Godot;

[GlobalClass]
public partial class Player : Actor {
  [Export]
  public VelocityStats velocityStats;
  [Export] public HealthComponent EntityHurtbox { get; set; }

  private CameraComponent camera;
  private StateMachine stateMachine;
  private VelocityComponent velocityComponent;
  private InputComponent inputComponent;

  private InputPackage input = new();

  public override void _Ready() {
    Input.MouseMode = Input.MouseModeEnum.Captured;

    camera = GetComponent(typeof(CameraComponent)) as CameraComponent;
    stateMachine = GetComponent(typeof(StateMachine)) as StateMachine;
    inputComponent = GetComponent(typeof(InputComponent)) as InputComponent;
    velocityComponent =
      GetComponent(typeof(VelocityComponent)) as VelocityComponent;

    stateMachine.actorVelocityStats = velocityStats;
    stateMachine.actor = this;

    velocityComponent.stats = velocityStats;

    if(EntityHurtbox != null){
      EntityHurtbox.Died += OnDeath;

    }
  }

   private void OnDeath()
    {
        GD.Print($"{Name} wird aus der Szene entfernt.");
        QueueFree(); 
    }
  public override void _Process(double delta) {
    input = inputComponent.GetInput;
    stateMachine.input = input;
  }

  public override void _PhysicsProcess(double delta) {
    Vector3 rotation = Rotation;
    rotation.Y = camera.Direction.Y;
    Rotation = rotation;

    Vector3 inputDirection = new(input.direction.X, 0.0f, input.direction.Y);
    Vector3 direction = (Transform.Basis * inputDirection).Normalized();
    direction = direction.Rotated(UpDirection, camera.Direction.Y);

    if(direction == Vector3.Zero) { velocityComponent.Decelerate(); }
    else { velocityComponent.AccelerateInDirection(direction); }

    velocityComponent.ApplyGravity(GetGravity() * (float)delta);

    velocityComponent.Move(this);
  }

  public override void _UnhandledInput(InputEvent @event) {
    if(Input.IsActionJustPressed("exit")) { GetTree().Quit(); }

    if(Input.IsActionJustPressed("mouse_capture")) {
      Input.MouseMode =
        Input.MouseMode == Input.MouseModeEnum.Captured ?
        Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }
  }
}

