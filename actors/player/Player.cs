using Godot;
public enum ItemSoundType {
    Page,
    Ammo,
    Empty
}
public enum PlayerSoundType {
    Footstep
}
public partial class Player : Actor, IHitable {
  private float footstepTimer = 0.0f;
private float footstepInterval = 0.35f;
private GodotObject footstepInstance;
  private bool isFootstepPlaying = false;
  private VelocityComponent velocityComponent;
  private CameraComponent camera;
  private InputComponent inputComponent;
  private StateMachine stateMachine;
  private HealthComponent healthComponent;
  private InventoryComponent inventoryComponent;
  private InsanityComponent insanityComponent;

  [Export] private Weapon[] weapons;
  private Weapon activeWeapon;
  private int activeWeaponIndex = 0;
  private bool switchingWeapon;

  private RayCast3D pickupCast;

  private bool hoveringPickup;
  private Pickup? hoveredPickup;

  private ProgressBar healthBar;
  private ProgressBar insanityMeter;

  private Label ammoDisplay;
  private Label potionCount;
  private InventoryUI inventoryUI;

  private HealingAnimation healingAnim;
  private bool isHealing;

  private System.Collections.Generic.Dictionary<WeaponType, int> savedAmmo = new();
  [Signal] public delegate void InteractingEventHandler();

  public override void _Ready() {
    base._Ready();

    var fmodServer = Engine.GetSingleton("FmodServer");
    if (fmodServer != null) {
      footstepInstance = fmodServer.Call("create_event_instance", "event:/Walk_Timeline").As<GodotObject>();
    }
    Input.MouseMode = Input.MouseModeEnum.Captured;

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
    inventoryUI.Visible = false;

    GD.Print("Player ready. activeWeaponIndex=" + activeWeaponIndex + ", weaponsCount=" + weapons.Length + ", active=" + (activeWeapon != null ? activeWeapon.Name : "null"));
    for(int i = 0; i < weapons.Length; i++) {
      string path = weaponsArrayPath(i);
      GD.Print("  weapon[" + i + "] path=" + path + ", valid=" + (weapons[i] != null) + ", name=" + (weapons[i] != null ? weapons[i].Name : "null") + ", visible=" + (weapons[i] != null ? weapons[i].Visible : false));
      if(weapons[i] != null && weapons[i].info != null) {
        GD.Print("      ammo=" + weapons[i].CurrentAmmo + "/" + weapons[i].info.magazineSize + ", ammoType=" + weapons[i].AmmoType);
      }
    }
    GD.Print("Input weapon1=" + input.weapon1 + ", weapon2=" + input.weapon2);
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
      if(input.shoot && !switchingWeapon) {
        GD.Print("[Diag] shoot pressed, active=" + (activeWeapon != null ? activeWeapon.Name : "null"));
        activeWeapon.Shoot();
      }
      if(input.reload && !switchingWeapon) { activeWeapon.Reload(); }
      if(input.usePotion && !isHealing) {
        if(healthComponent.CurrentHealth < healthComponent.maxHealth) {
          if(inventoryComponent.AmountOf(ItemType.POTION) > 0) {
            isHealing = true;
            healingAnim?.PlayHeal();
          }
        }
      }

      // Weapon switching
      if(!switchingWeapon) {
        if(input.weapon1 && activeWeaponIndex != 0) {
          GD.Print("Switch requested: weapon1");
          StartWeaponSwitch(0);
        } else if(input.weapon2 && activeWeaponIndex != 1 && weapons.Length > 1) {
          GD.Print("Switch requested: weapon2, weaponsCount=" + weapons.Length + ", index1Null=" + (weapons[1] == null));
          StartWeaponSwitch(1);
        }
      }

      if(pickupCast.IsColliding()) {
        hoveringPickup = true;
        Area3D? collider = pickupCast.GetCollider() as Area3D;
        if(collider?.GetParent() is Pickup pickup) {
          if(pickup != hoveredPickup) {
            if(hoveredPickup != null) { hoveredPickup.hovering = false; }
            hoveredPickup = pickup;
            if(hoveredPickup != null) { hoveredPickup.hovering = true; }
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
              PlayItemSound(ItemSoundType.Page);
            }

            RedrawUI();
          }
        }
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
    if (footstepInstance != null && GodotObject.IsInstanceValid(footstepInstance)) {
        
        // Exakte Laufgeschwindigkeit am Boden ermitteln
        Vector3 flatVelocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
        float currentSpeed = flatVelocity.Length();

        if (currentSpeed > 0.1f && IsOnFloor()) {
            
            // Parameter ans Loop-Event in FMOD senden
            GD.Print(currentSpeed);
            footstepInstance.Call("set_parameter_by_name", "WalkSpeed", currentSpeed);

            // Sound starten, falls er noch pausiert ist
            if (!isFootstepPlaying) {
                footstepInstance.Call("start");
                isFootstepPlaying = true;
            }
        } 
        else {
            // Spieler steht oder springt -> Sound sanft stoppen
            if (isFootstepPlaying) {
                footstepInstance.Call("stop", 0); // 0 = FMOD_STUDIO_STOP_ALLOWFADEOUT
                isFootstepPlaying = false;
            }
        }
    }
    // ==========================================
    // --- Update weapon animation motion state ---
    UpdateWeaponMotion();
  }

  private void UpdateWeaponMotion() {
    if(activeWeapon == null || activeWeapon.weaponAnim == null) { return; }
    Vector2 flat = new(Velocity.X, Velocity.Z);
    float speed = flat.Length();
    bool sprinting = Input.IsActionPressed("sprint");
    activeWeapon.weaponAnim.SetMotionState(speed, sprinting);
    activeWeapon.weaponAnim.SetGrounded(IsOnFloor(), Velocity.Y);

  }

  public override void _UnhandledInput(InputEvent @event) {
    if(Input.IsActionJustPressed("exit")) { GetTree().Quit(); }

    if(Input.IsActionJustPressed("mouse_capture")) {
      Input.MouseMode =
        Input.MouseMode == Input.MouseModeEnum.Captured ?
        Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }

    // Feed mouse look velocity into weapon animation for inertia
    if(activeWeapon?.weaponAnim != null && @event is InputEventMouseMotion motion) {
      activeWeapon.weaponAnim.SetLookVelocity(motion.Relative);
    }

    // Fallback switching with number keys if Input Map actions are missing
    if(@event is InputEventKey keyEvent && keyEvent.Pressed) {
      if(keyEvent.Keycode == Key.Key1 && activeWeaponIndex != 0) {
        GD.Print("Fallback switch requested: 1");
        StartWeaponSwitch(0);
      }
      if(keyEvent.Keycode == Key.Key2 && activeWeaponIndex != 1 && weapons.Length > 1) {
        GD.Print("Fallback switch requested: 2");
        StartWeaponSwitch(1);
      }
    }
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

  private string weaponsArrayPath(int i) {
    try {
      if(i < 0 || i >= weapons.Length) { return "out-of-range"; }
      var path = weapons[i]?.GetPath();
      return path ?? "null";
    } catch { return "err"; }
  }

  private void StartWeaponSwitch(int index) {
    if(switchingWeapon || index == activeWeaponIndex || weapons[index] == null) {
      return;
    }
    switchingWeapon = true;

    Weapon next = weapons[index];
    Weapon current = activeWeapon;

    // --- ALTE WAFFE WEGSTECKEN ---
    if(current != null) {
      if (current.weaponAnim != null) {
          // 1. Signal temporär KAPPEN, damit der Reload-Bug unmöglich wird!
          current.weaponAnim.ReloadVisualComplete -= current.OnReloadVisualComplete;
          
          // 2. Jetzt die Animation sicher abwürgen
          current.weaponAnim.ForceFinishReload();
          
          // 3. Signal wieder verbinden (für das nächste Mal)
          current.weaponAnim.ReloadVisualComplete += current.OnReloadVisualComplete;
      }
      current.Reset();
      current.Visible = false;
      current.ProcessMode = ProcessModeEnum.Disabled;
    }

    // --- NEUE WAFFE ZIEHEN ---
    if(next != null) {
      if (next.weaponAnim != null) {
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

      activeWeapon.Shot -= RedrawAmmoUI;
      activeWeapon.Reloaded -= RedrawAmmoUI;
      activeWeapon.Shot += RedrawAmmoUI;
      activeWeapon.Reloaded += RedrawAmmoUI;

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
    if (activeWeapon == newWeapon) return;

   
    if (activeWeapon != null && activeWeapon.info != null) {
        savedAmmo[activeWeapon.info.type] = activeWeapon.CurrentAmmo;
        activeWeapon.Hide(); // Oder QueueFree(), falls du sie löschst
    }

   
    activeWeapon = newWeapon;
    activeWeapon.Show();

   
    if (savedAmmo.TryGetValue(activeWeapon.info.type, out int savedAmount)) {
       
        activeWeapon.SetCurrentAmmo(savedAmount);
    } 
    else {
       
        activeWeapon.SetCurrentAmmo(activeWeapon.info.magazineSize);
    }

    // UI aktualisieren
    RedrawAmmoUI();
}
  private string GetEventPathForType(ItemSoundType soundType) {
        switch (soundType) {
            case ItemSoundType.Page:
                return "event:/Pages_Interaction_Action";
            case ItemSoundType.Ammo:
                return "event:/Pages_Interaction_Action";
            default:
                return string.Empty;
        }
    }
  



  public void PlayItemSound(ItemSoundType soundType) {
    string eventPath = GetEventPathForType(soundType);

    if (!string.IsNullOrEmpty(eventPath)) {
        var fmodServer = Engine.GetSingleton("FmodServer");
        if (fmodServer != null) {
            
          
            fmodServer.Call("play_one_shot", eventPath);

            GD.Print($">>> FMOD 2D Sound abgespielt: {soundType} ({eventPath})");

        } else {
            GD.PrintErr(">>> FMOD Fehler: FmodServer-Singleton nicht gefunden!");
        }
    }
}
public override void _ExitTree() {
    base._ExitTree(); // Falls deine Actor-Klasse das braucht
    
    // ... deine bisherigen weaponAnim Abmeldungen ...

    // Sound hart stoppen und aufräumen, wenn der Player verschwindet
    if (footstepInstance != null && GodotObject.IsInstanceValid(footstepInstance)) {
      footstepInstance.Call("stop", 1); // 1 = FMOD_STUDIO_STOP_IMMEDIATE
      footstepInstance.Call("release");
    }
  }
}
