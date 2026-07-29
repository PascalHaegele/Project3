using FmodSharp;
using Godot;

public enum ItemSoundType {
  Page,
  Ammo,
  Potion
}

public enum PlayerSoundType {
  Footstep,
}

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
  private int activeWeaponIndex;
  private bool switchingWeapon;

  private RayCast3D pickupCast;

  private bool hoveringPickup;
  private Pickup? hoveredPickup;

  private Control hud;
  private ProgressBar healthBar;
  private ProgressBar insanityMeter;

  private Label ammoDisplay;
  private Label potionCount;
  private InventoryUI inventoryUI;

  private HealingAnimation healingAnim;
  private bool isHealing;

  [Export] private StringName footstepEventPath;
  private FmodEvent footstepEvent;

  private float footstepTimer;
  private float footstepInterval = 0.35f;
  private GodotObject footstepInstance;
  private bool isFootstepPlaying;

  [Signal] public delegate void InteractingEventHandler();

  public override void _Ready() {
    base._Ready();

    camera = GetComponent<CameraComponent>();
    inputComponent = GetComponent<InputComponent>();
    stateMachine = GetComponent<StateMachine>();
    velocityComponent = GetComponent<VelocityComponent>();
    healthComponent = GetComponent<HealthComponent>();
    inventoryComponent = GetComponent<InventoryComponent>();
    insanityComponent = GetComponent<InsanityComponent>();

    healingAnim = GetNode<HealingAnimation>("CameraPivot/HealingAnimation");
    if(healingAnim != null) {
      healingAnim.HealingComplete += OnHealingComplete;
    }

    healthComponent.HealthChanged += OnHealthChanged;

    insanityComponent.InsanityChanged += OnInsanityChanged;

    foreach(Weapon weapon in weapons) {
      weapon.Shot += RedrawAmmoUI;
      weapon.Reloaded += RedrawAmmoUI;
    }
    activeWeapon = weapons[0];

    pickupCast = GetNode<RayCast3D>("CameraPivot/PickupCast");

    hud = GetNode<Control>("HUD");

    healthBar = hud.GetNode<ProgressBar>("HealthBar");
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;

    insanityMeter = hud.GetNode<ProgressBar>("InsanityMeter");
    insanityMeter.MaxValue = insanityComponent.MaxInsanity;
    insanityMeter.Value = insanityComponent.CurrentInsanity;

    ammoDisplay = hud.GetNode<Label>("AmmoDisplay");
    RedrawAmmoUI();

    potionCount = hud.GetNode<Label>("PotionCount");
    RedrawPotionUI();

    // Setup InventoryUI
    inventoryUI = hud.GetNodeOrNull<InventoryUI>("InventoryUI");
    if(inventoryUI == null) {
      // Create it dynamically if not in scene
      inventoryUI = new InventoryUI();
      inventoryUI.Name = "InventoryUI";
      GetNode("HUD").AddChild(inventoryUI);
    }

    inventoryUI
      .Initialize(
        inventoryComponent,
        GetComponent<SocketComponent>(),
        activeWeapon
      );
    inventoryUI.Visible = false;

    var fmodServer = Engine.GetSingleton("FmodServer");
    if(fmodServer != null) {
      footstepInstance =
        fmodServer
          .Call("create_event_instance", "event:/Walk_Timeline")
          .As<GodotObject>();
    }

    if(footstepEventPath != null) {
      footstepEvent = FmodServerWrapper.CreateEventInstance(footstepEventPath);
      AddChild(footstepEvent);
    }
  }

  public override void _Process(double delta) {
    Debug.panel.AddProperty("Velocity", Velocity.ToString("f2"), 2);
  }

  public override void _PhysicsProcess(double delta) {
    input = inputComponent.GetInput();
    stateMachine.UpdateInput(input);

    if(input.openInventory) { inventoryUI?.Toggle(); }

    if(!inventoryUI.Visible) {
      if(input.interact) { EmitSignalInteracting(); }

      if(input.shoot && !switchingWeapon) { activeWeapon.Shoot(); }
      if(input.reload && !switchingWeapon) { activeWeapon.Reload(); }

      if(input.usePotion && !isHealing) {
        if(healthComponent.CurrentHealth < healthComponent.maxHealth) {
          if(inventoryComponent.AmountOf(ItemType.POTION) > 0) {
            isHealing = true;
            healingAnim?.PlayHeal();
          }
        }
      }

      if(input.weapon1 && activeWeaponIndex != 0) {
        StartWeaponSwitch(0);
      } else if(input.weapon2 && activeWeaponIndex != 1 && weapons.Length > 1) {
        StartWeaponSwitch(1);
      }
      if(input.switchWeapon) {
        StartWeaponSwitch((activeWeaponIndex + 1) % weapons.Length);
      }

      if(pickupCast.IsColliding()) {
        HandlePickup();
      } else {
        hoveringPickup = false;
        if(hoveredPickup != null) { hoveredPickup.hovering = false; }
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

    // ==========================================
    // GODOT TIMER FÜR SCHRITTE (KORRIGIERT!)
    // ==========================================

    // if(footstepInstance != null && IsInstanceValid(footstepInstance)) {
    //   // Exakte Laufgeschwindigkeit am Boden ermitteln
    //   Vector3 flatVelocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
    //   float currentSpeed = flatVelocity.Length();
    //
    //   if(currentSpeed > 0.1f && IsOnFloor()) {
    //     // Parameter ans Loop-Event in FMOD senden
    //     GD.Print(currentSpeed);
    //     _ = footstepInstance
    //       .Call("set_parameter_by_name", "WalkSpeed", currentSpeed);
    //
    //     // Sound starten, falls er noch pausiert ist
    //     if(!isFootstepPlaying) {
    //       _ = footstepInstance.Call("start");
    //       isFootstepPlaying = true;
    //     }
    //   } else {
    //     // Spieler steht oder springt -> Sound sanft stoppen
    //     if(isFootstepPlaying) {
    //       _ = footstepInstance.Call("stop", 0); // 0 = FMOD_STUDIO_STOP_ALLOWFADEOUT
    //       isFootstepPlaying = false;
    //     }
    //   }
    // }

    if(footstepEvent != null) {
      // Exakte Laufgeschwindigkeit am Boden ermitteln
      Vector3 flatVelocity = new(Velocity.X, 0.0f, Velocity.Z);
      float currentSpeed = flatVelocity.Length();

      if(currentSpeed > 0.1f && IsOnFloor()) {

        // Parameter ans Loop-Event in FMOD senden
        footstepEvent.SetParameterByName("WalkSpeed", currentSpeed);

        // Sound starten, falls er noch pausiert ist
        if(!isFootstepPlaying) {
          footstepEvent.Start();
          isFootstepPlaying = true;
        }
      } else {
        // Spieler steht oder springt -> Sound sanft stoppen
        if(isFootstepPlaying) {
          footstepEvent.Stop(FmodServerWrapper.FMOD_STUDIO_STOP_ALLOWFADEOUT);
          isFootstepPlaying = false;
        }
      }
    }

    // ==========================================
    // --- Update weapon animation motion state ---
    UpdateWeaponMotion();
  }

  public void ShowHUD() => hud.Visible = true;

  public void HideHUD() => hud.Visible = false;

  private void HandlePickup() {
    hoveringPickup = true;
    Area3D? collider = pickupCast.GetCollider() as Area3D;
    if(collider?.GetParent() is Pickup pickup) {
      if(pickup != hoveredPickup) {
        if(hoveredPickup != null) { hoveredPickup.hovering = false; }
        hoveredPickup = pickup;
        if(hoveredPickup != null) { hoveredPickup.hovering = true; }
      }
      if(input.interact) {
        hoveredPickup.QueueFree();
        inventoryComponent
          .AddItem(hoveredPickup.itemType, hoveredPickup.amount);

        // For pages, also add the PageData to the collected pages list
        if(
          hoveredPickup.itemType == ItemType.PAGE &&
          hoveredPickup.pageData != null
        ) {
          inventoryComponent.AddPageItem(hoveredPickup.pageData);
          PlayItemSound(ItemSoundType.Page);
        }
        else if (hoveredPickup.itemType == ItemType.POTION) {
          PlayItemSound(ItemSoundType.Potion);
        }
        else if (hoveredPickup.itemType == ItemType.R_AMMO || hoveredPickup.itemType == ItemType.S_AMMO ) {
          PlayItemSound(ItemSoundType.Ammo);
        }

        RedrawUI();
      }
    }
  }

  private void UpdateWeaponMotion() {
    if(activeWeapon == null || activeWeapon.weaponAnim == null) { return; }
    Vector2 flat = new(Velocity.X, Velocity.Z);
    float speed = flat.Length();
    bool sprinting = Input.IsActionPressed("sprint");
    activeWeapon.weaponAnim.SetLookVelocity(camera.Motion);
    activeWeapon.weaponAnim.SetMotionState(speed, sprinting);
    activeWeapon.weaponAnim.SetGrounded(IsOnFloor(), Velocity.Y);
  }

  public void RecieveHit(HitInfo info) {
    healthComponent.TakeDamage(info.damage);
    insanityComponent.AddInsanity(10.0f);
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

  private void OnHealingComplete() {
    if(inventoryComponent.RemoveItem(ItemType.POTION)) {
      healthComponent.Heal(20.0f);
      RedrawPotionUI();
    }
    isHealing = false;
  }

  private void StartWeaponSwitch(int index) {
    if(
      switchingWeapon ||
      index == activeWeaponIndex ||
      weapons[index] == null
    ) { return; }
    switchingWeapon = true;

    Weapon next = weapons[index];
    Weapon current = activeWeapon;

    // --- ALTE WAFFE WEGSTECKEN ---
    if(current != null) {
      if(current.weaponAnim != null) {
        // 1. Signal temporär KAPPEN, damit der Reload-Bug unmöglich wird!
        current.weaponAnim.ReloadVisualComplete -=
          current.OnReloadVisualComplete;

        // 2. Jetzt die Animation sicher abwürgen
        current.weaponAnim.ForceFinishReload();

        // 3. Signal wieder verbinden (für das nächste Mal)
        current.weaponAnim.ReloadVisualComplete +=
          current.OnReloadVisualComplete;
      }
      current.Reset();
      current.Visible = false;
      current.ProcessMode = ProcessModeEnum.Disabled;
    }

    // --- NEUE WAFFE ZIEHEN ---
    if(next != null) {
      if(next.weaponAnim != null) {
        // 1. Auch hier das Signal kappen
        next.weaponAnim.ReloadVisualComplete -= next.OnReloadVisualComplete;

        // 2. Animation sicher zurücksetzen
        next.weaponAnim.SetWeaponNode(next);
        next.weaponAnim.ForceFinishReload();

        // 3. Signal wieder verbinden
        next.weaponAnim.ReloadVisualComplete += next.OnReloadVisualComplete;
      }

      next.Reset();
      next.Visible = true;
      next.ProcessMode = ProcessModeEnum.Inherit;

      activeWeapon = next;
      activeWeaponIndex = index;

      RedrawAmmoUI();
    }

    switchingWeapon = false;
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

  public void SwitchWeapon(Weapon newWeapon) {
    if(activeWeapon == newWeapon) { return; }

    if(activeWeapon != null && activeWeapon.info != null) {
      activeWeapon.Hide();
    }

    activeWeapon = newWeapon;
    activeWeapon.Show();

    RedrawAmmoUI();
  }

  private string GetEventPathForType(ItemSoundType soundType) {
    switch(soundType) {
      case ItemSoundType.Page:
        return "event:/Pages_Interaction_Action";
      case ItemSoundType.Ammo:
        return "event:/Ammo_Interaction_Action";
      case ItemSoundType.Potion:
        return "event:/Potion_Interaction_Action";
      default:
        return string.Empty;
    }
  }

  public void PlayItemSound(ItemSoundType soundType) {
    string eventPath = GetEventPathForType(soundType);

    if(!string.IsNullOrEmpty(eventPath)) {
      FmodServerWrapper.PlayOneShot(eventPath);
      FmodServerWrapper.PlayOneShotAttached(eventPath, this);
    }

    if(!string.IsNullOrEmpty(eventPath)) {
      var fmodServer = Engine.GetSingleton("FmodServer");
      if(fmodServer != null) {
        _ = fmodServer.Call("play_one_shot", eventPath);

        GD.Print($">>> FMOD 2D Sound abgespielt: {soundType} ({eventPath})");

      } else {
        GD.PrintErr(">>> FMOD Fehler: FmodServer-Singleton nicht gefunden!");
      }
    }
  }

  public override void _ExitTree() {
    // Sound hart stoppen und aufräumen, wenn der Player verschwindet
    if (footstepInstance != null && IsInstanceValid(footstepInstance)) {
      _ = footstepInstance.Call("stop", 1); // 1 = FMOD_STUDIO_STOP_IMMEDIATE
      _ = footstepInstance.Call("release");
    }
  }
}
