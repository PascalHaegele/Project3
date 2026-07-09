using Godot;

public partial class Player : Actor {
  private VelocityComponent velocityComponent;
  private CameraComponent camera;
  private InputComponent inputComponent;
  private StateMachine stateMachine;
  private HealthComponent healthComponent;

  [Export] private Weapon weapon;

  private Label ammoDisplay;
  private ProgressBar healthBar;

  [Signal] public delegate void InteractingEventHandler();

  public override void _Ready() {
    base._Ready();

    Input.MouseMode = Input.MouseModeEnum.Captured;

    camera = GetComponent<CameraComponent>();
    inputComponent = GetComponent<InputComponent>();
    stateMachine = GetComponent<StateMachine>();
    velocityComponent = GetComponent<VelocityComponent>();
    healthComponent = GetComponent<HealthComponent>();

    healthComponent.HealthChanged += OnHealthChanged;

    weapon.Shot += OnWeaponShot;
    weapon.Reloaded += OnWeaponReloaded;

    ammoDisplay = GetNode<Label>("HUD/AmmoDisplay");
    ammoDisplay.Text =
      weapon.CurrentAmmo.ToString() + " / " + weapon.info.magazineSize;

    healthBar = GetNode<ProgressBar>("HUD/HealthBar");
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;
  }

  public override void _Process(double delta) {
    input = inputComponent.GetInput();
    stateMachine.input = input;

    if(input.interact) { EmitSignalInteracting(); }
    if(input.shoot) { weapon.Shoot(); }
    if(input.reload) { weapon.Reload(); }

    Debug
      .panel
      .AddProperty("Velocity", Velocity.ToString("f2"), 2);
  }

  public override void _PhysicsProcess(double delta) {
    // --- Rotation ---
    if(!Mathf.IsEqualApprox(camera.Direction.Y, 0.0f)) {
      RotateY(camera.Direction.Y);
      Vector3 camDir = camera.Direction;
      camDir.Y = 0.0f;
      camera.Direction = camDir;
    }

    // --- Movement direction ---
    Vector3 inputDirection = new(input.direction.X, 0.0f, input.direction.Y);
    Direction = (Transform.Basis * inputDirection).Normalized();
    Direction = Direction.Rotated(UpDirection, camera.Direction.Y);

    // --- Gravity & move ---
    if(!IsOnFloor()) {
      velocityComponent.AddVelocityInDirection(GetGravity() * (float)delta);
    }
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

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = healthComponent.CurrentHealth;
  }

  private void OnWeaponShot() {
    ammoDisplay.Text =
      weapon.CurrentAmmo.ToString() + " / " + weapon.info.magazineSize;
  }

  private void OnWeaponReloaded() {
    ammoDisplay.Text =
      weapon.CurrentAmmo.ToString() + " / " + weapon.info.magazineSize;
  }
}

