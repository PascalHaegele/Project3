using Godot;

[GlobalClass]
public partial class Player : Actor {
  [Export]
  public VelocityStats velocityStats;
  [Export] public HealthComponent EntityHurtbox { get; set; }

  [ExportGroup("Jump")]
  [Export] public float JumpVelocity = 8f;

  [ExportGroup("Dash")]
  [Export] public float DashDistance = 8f;
  [Export] public float DashDuration = 0.2f;
  [Export] public float DashCooldown = 0.8f;

  public HealthComponent healthComponent;

  private CameraComponent camera;
  private InputComponent inputComponent;
  private StateMachine stateMachine;
  private VelocityComponent velocityComponent;
  private InventoryComponent inventoryComponent;

  private InputPackage input = new();

  // Dash runtime
  private double _dashTimer = 0f;
  private double _dashCooldownTimer = 0f;
  private Vector3 _dashDirection = Vector3.Zero;
  private bool _isDashing = false;

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
    // --- Cooldown timer ---
    if(_dashCooldownTimer > 0f)
      _dashCooldownTimer -= delta;

    // --- Dash active? Override everything ---
    if(_isDashing) {
      _dashTimer -= delta;

      velocityComponent.velocity = _dashDirection * (DashDistance / DashDuration);
      velocityComponent.ApplyGravity(GetGravity() * (float)delta);
      velocityComponent.Move(this);

      if(_dashTimer <= 0f) { EndDash(); }
      return;
    }

    // --- Rotation ---
    Vector3 rotation = Rotation;
    rotation.Y = camera.Direction.Y;
    Rotation = rotation;

    // --- Movement direction ---
    Vector3 inputDirection = new(input.direction.X, 0.0f, input.direction.Y);
    Vector3 direction = (Transform.Basis * inputDirection).Normalized();
    direction = direction.Rotated(UpDirection, camera.Direction.Y);

    if(direction == Vector3.Zero) { velocityComponent.Decelerate(); }
    else { velocityComponent.AccelerateInDirection(direction); }

    // --- Jump ---
    if(input.jump && IsOnFloor()) {
      velocityComponent.velocity.Y = JumpVelocity;
      GD.Print("Jump!");
    }

    // --- Dash (start) ---
    if(input.dash && _dashCooldownTimer <= 0f && !_isDashing) {
      StartDash(direction);
      return;
    }

    // --- Gravity & move ---
    velocityComponent.ApplyGravity(GetGravity() * (float)delta);
    velocityComponent.Move(this);
  }

  private void StartDash(Vector3 direction) {
    _dashDirection = direction;

    if(_dashDirection == Vector3.Zero) {
      // Dash forward if no directional input
      _dashDirection = -GlobalTransform.Basis.Z;
      _dashDirection.Y = 0f;
    }

    _dashDirection.Y = 0f;
    _dashDirection = _dashDirection.Normalized();

    _isDashing = true;
    _dashTimer = DashDuration;

    GD.Print("Dash!");
  }

  private void EndDash() {
    _isDashing = false;
    _dashCooldownTimer = DashCooldown;
    _dashTimer = 0f;
    velocityComponent.velocity = Vector3.Zero;
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

