using Godot;

public partial class Chest : StaticBody3D, IInteractable {
  private PackedScene pickup;

  private AnimationPlayer animationPlayer;
  private bool opened = false;
  private string[] availableAnimations;

  public override void _Ready() {
    // Load pickup scene directly to ensure it's available
    pickup = GD.Load<PackedScene>("res://objects/items/test_pickup.tscn");
    if (pickup == null) {
      GD.PrintErr("Chest: Could not load test_pickup.tscn!");
    }
    // Find AnimationPlayer from the imported GLB model
    var model = GetNode<Node3D>("Model");
    if (model.HasNode("AnimationPlayer")) {
      animationPlayer = model.GetNode<AnimationPlayer>("AnimationPlayer");
    } else {
      // Fallback: search recursively
      animationPlayer = GetAnimationPlayerRecursive(model);
    }

    if (animationPlayer != null) {
      availableAnimations = animationPlayer.GetAnimationList();
      GD.Print($"Chest animations found: {string.Join(", ", availableAnimations)}");
    } else {
      GD.PrintErr("Chest: No AnimationPlayer found!");
    }
  }

  private AnimationPlayer GetAnimationPlayerRecursive(Node node) {
    foreach (Node child in node.GetChildren()) {
      if (child is AnimationPlayer ap)
        return ap;
      var found = GetAnimationPlayerRecursive(child);
      if (found != null)
        return found;
    }
    return null;
  }

  public void Interact(Player player) {
    if (opened) return;
    opened = true;

    if (animationPlayer == null) {
      GD.PrintErr("Chest: No AnimationPlayer available.");
      return;
    }

    // Try to find a matching animation by keywords
    string animName = FindBestAnimation();
    if (animName != null) {
      animationPlayer.Play(animName);
      GD.Print($"Chest.Interact: Playing animation '{animName}'");
    } else {
      GD.PrintErr("Chest: No suitable animation found.");
    }

    if (pickup == null) {
      GD.PrintErr("Chest: pickup is still null after _Ready load. Trying UID load...");
      pickup = GD.Load<PackedScene>("uid://dudmf7poy21iv");
      if (pickup == null) {
        GD.PrintErr("Chest: Finally no pickup available.");
        return;
      }
      GD.Print("Chest:Loaded pickup via UID.");
    }

    // Spawn pickup from LootSpawn marker above the chest
    Marker3D lootSpawn = GetNodeOrNull<Marker3D>("LootSpawn");
    if (lootSpawn == null) {
      GD.PrintErr("Chest: LootSpawn marker not found!");
      return;
    }

    Vector3 baseSpawnPos = lootSpawn.GlobalPosition;

    // --- Drop 1: Page (always drop for testing) ---
    PageData page = GeneratePageDrop();
    GD.Print($"Chest: GeneratePageDrop returned: {(page != null ? page.PageName : "null")}");
    if (page == null) {
      GD.PrintErr("Chest: PageDatabase not ready or empty! Using fallback page.");
      page = new PageData {
        PageName = "Test Page",
        Description = "A test page dropped from chest.",
        Rarity = "Common"
      };
    }
    {
      Vector3 pagePos = baseSpawnPos;
      pagePos.X += (float)GD.RandRange(-0.4, 0.4);
      pagePos.Z += (float)GD.RandRange(-0.4, 0.4);

      Pickup pagePickup = pickup.Instantiate<Pickup>();
      GetParent().AddChild(pagePickup);
      pagePickup.GlobalPosition = pagePos;
      pagePickup.itemType = ItemType.PAGE;
      pagePickup.pageData = page;
      pagePickup.ApplyImpulse(new Vector3(
        (float)GD.RandRange(-0.3, 0.3),
        2.0f,
        (float)GD.RandRange(-0.3, 0.3)
      ));
      GD.Print($"Chest dropped page: {page.PageName} [{page.Rarity}]");
    }

    // --- Drop 2: Potion (always drop for testing) ---
    Vector3 potionPos = baseSpawnPos;
    potionPos.X += (float)GD.RandRange(-0.4, 0.4);
    potionPos.Z += (float)GD.RandRange(-0.4, 0.4);

    Pickup potionPickup = pickup.Instantiate<Pickup>();
    GetParent().AddChild(potionPickup);
    potionPickup.GlobalPosition = potionPos;
    potionPickup.itemType = ItemType.POTION;
    potionPickup.ApplyImpulse(new Vector3(
      (float)GD.RandRange(-0.3, 0.3),
      2.0f,
      (float)GD.RandRange(-0.3, 0.3)
    ));
    GD.Print("Chest dropped potion.");

    // --- Drop 3: Ammo Revolver (always drop for testing) ---
    Vector3 ammoRevPos = baseSpawnPos;
    ammoRevPos.X += (float)GD.RandRange(-0.4, 0.4);
    ammoRevPos.Z += (float)GD.RandRange(-0.4, 0.4);

    Pickup ammoRevPickup = pickup.Instantiate<Pickup>();
    GetParent().AddChild(ammoRevPickup);
    ammoRevPickup.GlobalPosition = ammoRevPos;
    ammoRevPickup.itemType = ItemType.AMMUNITION;
    ammoRevPickup.ApplyImpulse(new Vector3(
      (float)GD.RandRange(-0.3, 0.3),
      2.0f,
      (float)GD.RandRange(-0.3, 0.3)
    ));
    GD.Print("Chest dropped ammo_rev.");

    // --- Drop 4: Ammo Shot (always drop for testing) ---
    Vector3 ammoShotPos = baseSpawnPos;
    ammoShotPos.X += (float)GD.RandRange(-0.4, 0.4);
    ammoShotPos.Z += (float)GD.RandRange(-0.4, 0.4);

    Pickup ammoShotPickup = pickup.Instantiate<Pickup>();
    GetParent().AddChild(ammoShotPickup);
    ammoShotPickup.GlobalPosition = ammoShotPos;
    ammoShotPickup.itemType = ItemType.AMMUNITION;
    ammoShotPickup.ApplyImpulse(new Vector3(
      (float)GD.RandRange(-0.3, 0.3),
      2.0f,
      (float)GD.RandRange(-0.3, 0.3)
    ));
    GD.Print("Chest dropped ammo_shot.");
  }

  private string FindBestAnimation() {
    if (availableAnimations == null || availableAnimations.Length == 0)
      return null;

    // Try common animation names for opening/closing
    string[] openKeywords = { "open", "Open", "OPEN", "close", "Close", "closeAction", "CloseAction", "action", "Action" };
    foreach (string anim in availableAnimations) {
      foreach (string keyword in openKeywords) {
        if (anim.Contains(keyword))
          return anim;
      }
    }

    // If no keyword match found, just use the first available animation
    return availableAnimations[0];
  }

  /// <summary>
  /// Generates a random page from the database.
  /// </summary>
  private PageData GeneratePageDrop() {
    if (PageDatabase.PageCount <= 0) return null;

    System.Collections.Generic.List<string> allPages = PageDatabase.GetAllPageNames();
    if (allPages.Count <= 0) return null;

    int randomIndex = (int)(GD.Randi() % (ulong)allPages.Count);
    return PageDatabase.GetPage(allPages[randomIndex]);
  }
}