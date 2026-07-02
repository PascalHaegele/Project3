using Godot;

[GlobalClass]
public partial class Player : Actor {
  [Export]
  public VelocityStats velocityStats;
  [Export] public HealthComponent EntityHurtbox { get; set; }

  public HealthComponent healthComponent;

  private CameraComponent camera;
  private InputComponent inputComponent;
  private StateMachine stateMachine;
  private VelocityComponent velocityComponent;
  private InventoryComponent inventoryComponent;

  private InputPackage input = new();

  [Signal]
  public delegate void InteractingEventHandler();

  public override void _Ready() {
    Input.MouseMode = Input.MouseModeEnum.Captured;

    healthComponent = GetComponent(typeof(HealthComponent)) as HealthComponent;

    camera = GetComponent(typeof(CameraComponent)) as CameraComponent;
    inputComponent = GetComponent(typeof(InputComponent)) as InputComponent;
    stateMachine = GetComponent(typeof(StateMachine)) as StateMachine;
    velocityComponent =
      GetComponent(typeof(VelocityComponent)) as VelocityComponent;
    inventoryComponent =
      GetComponent(typeof(InventoryComponent)) as InventoryComponent;

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
    input = inputComponent.GetInput();
    stateMachine.input = input;

    if(input.interact) { _ = EmitSignal(SignalName.Interacting); }
    if(input.openInventory) { inventoryComponent.PrintInventory(); }
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

    if(Input.IsKeyPressed(Key.F)) { inventoryComponent.AddItem("ITEM_0"); }
    if(Input.IsKeyPressed(Key.G)) { inventoryComponent.AddItem("ITEM_1"); }
    if(Input.IsKeyPressed(Key.H)) { _ = inventoryComponent.RemoveItem("ITEM_0"); }
    if(Input.IsKeyPressed(Key.J)) { inventoryComponent.ClearInventory(); }
  }
}

