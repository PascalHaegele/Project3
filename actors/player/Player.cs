using Godot;

public partial class Player : Actor, IHitable {
  private VelocityComponent velocityComponent;
  private CameraComponent camera;
  private InputComponent inputComponent;
  private StateMachine stateMachine;
  private HealthComponent healthComponent;
  private InventoryComponent inventoryComponent;
  private InsanityComponent insanityComponent;

  [Export] private Weapon[] weapons;
  private Weapon activeWeapon;

  private RayCast3D pickupCast;

  private bool hoveringPickup;
  private Pickup? hoveredPickup;

  private ProgressBar healthBar;
  private ProgressBar insanityMeter;

  private Label ammoDisplay;
  private Label potionCount;
  private InventoryUI inventoryUI;

  [Signal] public delegate void InteractingEventHandler();

  public override void _Ready() {
    base._Ready();

    Input.MouseMode = Input.MouseModeEnum.Captured;

    camera = GetComponent<CameraComponent>();
    inputComponent = GetComponent<InputComponent>();
    stateMachine = GetComponent<StateMachine>();
    velocityComponent = GetComponent<VelocityComponent>();
    healthComponent = GetComponent<HealthComponent>();
    inventoryComponent = GetComponent<InventoryComponent>();
    insanityComponent = GetComponent<InsanityComponent>();

    healthComponent.HealthChanged += OnHealthChanged;

    insanityComponent.InsanityChanged += OnInsanityChanged;

    activeWeapon = weapons[0];
    activeWeapon.Shot += RedrawAmmoUI;
    activeWeapon.Reloaded += RedrawAmmoUI;

    pickupCast = GetNode<RayCast3D>("CameraPivot/PickupCast");

    healthBar = GetNode<ProgressBar>("HUD/HealthBar");
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;

    insanityMeter = GetNode<ProgressBar>("HUD/InsanityMeter");
    insanityMeter.MaxValue = insanityComponent.MaxInsanity;
    insanityMeter.Value = insanityComponent.CurrentInsanity;

    ammoDisplay = GetNode<Label>("HUD/AmmoDisplay");
    RedrawAmmoUI();

    potionCount = GetNode<Label>("HUD/PotionCount");
    RedrawPotionUI();

    // Setup InventoryUI
    inventoryUI = GetNodeOrNull<InventoryUI>("HUD/InventoryUI");
    if(inventoryUI == null) {
      // Create it dynamically if not in scene
      inventoryUI = new InventoryUI();
      inventoryUI.Name = "InventoryUI";
      GetNode("HUD").AddChild(inventoryUI);
    }
    inventoryUI
      .Initialize(inventoryComponent, GetComponent<SocketComponent>(), activeWeapon);
  }

  public override void _Process(double delta) {
    Debug
      .panel
      .AddProperty("Velocity", Velocity.ToString("f2"), 2);
  }

  public override void _PhysicsProcess(double delta) {
    input = inputComponent.GetInput();
    stateMachine.UpdateInput(input);

    if(input.openInventory) { inventoryUI?.Toggle(); }
    if(!inventoryUI.Visible) {
      if(input.interact) { EmitSignalInteracting(); }
      if(input.shoot) { activeWeapon.Shoot(); }
      if(input.reload) { activeWeapon.Reload(); }
      if(input.usePotion) {
        if(healthComponent.CurrentHealth < healthComponent.maxHealth) {
          if(inventoryComponent.RemoveItem(ItemType.POTION)) {
            healthComponent.Heal(20.0f);
            RedrawPotionUI();
          }
        }
      }

      if(pickupCast.IsColliding()) {
        hoveringPickup = true;
        Area3D? collider = pickupCast.GetCollider() as Area3D;
        if(collider?.GetParent() is Pickup pickup) {
          if(pickup != hoveredPickup) {
            hoveredPickup?.hovering = false;
            hoveredPickup = pickup;
            hoveredPickup?.hovering = true;
          }
          if(input.interact) {
            GD.Print($"Interacted with {hoveredPickup.Name}");
            hoveredPickup.QueueFree();
            inventoryComponent
              .AddItem(hoveredPickup.itemType, hoveredPickup.amount);

            // For pages, also add the PageData to the collected pages list
            if(
              hoveredPickup.itemType == ItemType.PAGE &&
              hoveredPickup.pageData != null
            ) {
              inventoryComponent.AddPageItem(hoveredPickup.pageData);
            }

            RedrawUI();
          }
        }
      } else {
        hoveringPickup = false;
        hoveredPickup?.hovering = false;
        hoveredPickup = null;
      }
    }

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

  public void RecieveHit(HitInfo info) {
    healthComponent.TakeDamage(info.damage);
    insanityComponent.AddInsanity(5.0f);
  }

  public void Reset() {
    healthComponent.Reset();
    insanityComponent.ResetInsanity();
    inventoryComponent.Reset();
    foreach(Weapon weapon in weapons) { weapon.Reset(); }

    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;

    insanityMeter.MaxValue = insanityComponent.MaxInsanity;
    insanityMeter.Value = insanityComponent.CurrentInsanity;
  }

  private void RedrawUI() {
    RedrawPotionUI();
    RedrawAmmoUI();
  }

  private void RedrawPotionUI() {
    potionCount.Text = "P : " + inventoryComponent.AmountOf(ItemType.POTION);
  }

  private void RedrawAmmoUI() {
    ammoDisplay.Text =
      activeWeapon.CurrentAmmo.ToString() +
      " / " +
      inventoryComponent.AmountOf(activeWeapon.AmmoType);
  }

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = newHealth;
  }

  private void OnInsanityChanged(float insanity) {
    insanityMeter.Value = insanity;
  }
}
