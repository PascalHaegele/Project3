using Godot;
using System.Collections.Generic;

public partial class Chest : StaticBody3D, IInteractable {
  private PackedScene pickup;

  private AnimationPlayer animationPlayer;
  private bool opened = false;
  private string[] availableAnimations;

  public override void _Ready() {
    // Load pickup scene directly to ensure it's available
    pickup = GD.Load<PackedScene>("res://objects/items/test_pickup.tscn");
    if(pickup == null) {
      GD.PrintErr("Chest: Could not load test_pickup.tscn!");
    }
    // Find AnimationPlayer from the imported GLB model
    Node3D model = GetNode<Node3D>("Model");
    if(model.HasNode("AnimationPlayer")) {
      animationPlayer = model.GetNode<AnimationPlayer>("AnimationPlayer");
    } else {
      // Fallback: search recursively
      animationPlayer = GetAnimationPlayerRecursive(model);
    }

    if(animationPlayer != null) {
      availableAnimations = animationPlayer.GetAnimationList();
      GD.Print($"Chest animations found: {string.Join(", ", availableAnimations)}");
    } else {
      GD.PrintErr("Chest: No AnimationPlayer found!");
    }
  }

  private AnimationPlayer GetAnimationPlayerRecursive(Node node) {
    foreach(Node child in node.GetChildren()) {
      if(child is AnimationPlayer ap) { return ap; }
      AnimationPlayer found = GetAnimationPlayerRecursive(child);
      if(found != null) { return found; }
    }
    return null;
  }

  public void Interact(Player player) {
    if(opened) { return; }

    opened = true;

    if(animationPlayer == null) {
      GD.PrintErr("Chest: No AnimationPlayer available.");
    } else {
      string animationName = FindBestAnimation();
      if(!string.IsNullOrEmpty(animationName)) {
        animationPlayer.Play(animationName);
        GD.Print($"Chest: Playing animation '{animationName}'.");
      }
    }

    if(pickup == null) {
      GD.PrintErr("Chest: pickup is still null after _Ready load. Trying UID load...");
      pickup = GD.Load<PackedScene>("uid://dudmf7poy21iv");
      if(pickup == null) {
        GD.PrintErr("Chest: Finally no pickup available.");
        return;
      }
      GD.Print("Chest:Loaded pickup via UID.");
    }

    // Spawn pickup from LootSpawn marker above the chest
    Marker3D lootSpawn = GetNodeOrNull<Marker3D>("LootSpawn");
    if(lootSpawn == null) {
      GD.PrintErr("Chest: LootSpawn marker not found!");
      return;
    }

    Vector3 baseSpawnPos = lootSpawn.GlobalPosition;
    var rng = new RandomNumberGenerator();
    rng.Randomize();

    GD.Print("Chest: Generating loot...");

    // --- GUARANTEED LOOT: 1 Ammo Pack (50/50 Revolver or Shotgun) ---
    string guaranteedAmmoType = rng.Randf() < 0.5f ? "Revolver" : "Shotgun";
    DropAmmo(guaranteedAmmoType, baseSpawnPos, rng);
    GD.Print($"Chest dropped guaranteed ammo: {guaranteedAmmoType}");

    // --- RANDOM LOOT ---

    // 60% chance: Drop 1 Health Potion
    if (rng.Randf() < 0.6f) {
      DropItem(ItemType.POTION, null, baseSpawnPos, rng);
      GD.Print("Chest dropped random loot: Health Potion");
    }

    // 45% chance: Drop 1 Page
    if (rng.Randf() < 0.45f) {
      PageData page = GeneratePageDrop();
      DropItem(ItemType.PAGE, page, baseSpawnPos, rng);
      GD.Print("Chest dropped random loot: Page");
    }

    // 20% chance: Drop an extra Ammo Pack (random type)
    if (rng.Randf() < 0.2f) {
      string extraAmmoType = rng.Randf() < 0.5f ? "Revolver" : "Shotgun";
      DropAmmo(extraAmmoType, baseSpawnPos, rng);
      GD.Print($"Chest dropped random loot: Extra Ammo Pack ({extraAmmoType})");
    }

    GD.Print("Chest: Loot generation complete.");
  }

  /// <summary>
  /// Drops an ammo pickup with the correct model visibility.
  /// </summary>
  private void DropAmmo(string ammoType, Vector3 baseSpawnPos, RandomNumberGenerator rng) {
    Pickup pickupInstance = pickup.Instantiate<Pickup>();
    GetParent().AddChild(pickupInstance);

    Vector3 pos = baseSpawnPos;
    pos.X += (float)GD.RandRange(-0.4, 0.4);
    pos.Z += (float)GD.RandRange(-0.4, 0.4);
    pickupInstance.GlobalPosition = pos;

    pickupInstance.itemType = ItemType.R_AMMO;
    pickupInstance.pageData = null;

    // Use same impulse as original chest
    pickupInstance.ApplyImpulse(new Vector3(
      (float)GD.RandRange(-0.3, 0.3),
      2.0f,
      (float)GD.RandRange(-0.3, 0.3)
    ));
  }

  /// <summary>
  /// Drops a general item (potion or page) using the same spawn pattern as the original chest.
  /// </summary>
  private void DropItem(ItemType type, PageData page, Vector3 baseSpawnPos, RandomNumberGenerator rng) {
    Pickup pickupInstance = pickup.Instantiate<Pickup>();
    GetParent().AddChild(pickupInstance);

    Vector3 pos = baseSpawnPos;
    pos.X += (float)GD.RandRange(-0.4, 0.4);
    pos.Z += (float)GD.RandRange(-0.4, 0.4);
    pickupInstance.GlobalPosition = pos;

    pickupInstance.itemType = type;
    pickupInstance.pageData = page;

    // Use same impulse as original chest
    pickupInstance.ApplyImpulse(new Vector3(
      (float)GD.RandRange(-0.3, 0.3),
      2.0f,
      (float)GD.RandRange(-0.3, 0.3)
    ));
  }

  private string FindBestAnimation() {
    if(availableAnimations == null || availableAnimations.Length == 0) {
      return null;
    }

    // Try common animation names for opening/closing
    string[] openKeywords = ["open", "Open", "OPEN", "close", "Close", "closeAction", "CloseAction", "action", "Action"];
    foreach(string anim in availableAnimations) {
      foreach(string keyword in openKeywords) {
        if(anim.Contains(keyword)) { return anim; }
      }
    }

    // If no keyword match found, just use the first available animation
    return availableAnimations[0];
  }

  /// <summary>
  /// Generates a random page from the database, with rarity weights:
  /// 70% Common, 25% Magic, 5% Rare.
  /// </summary>
  private PageData GeneratePageDrop() {
    if(PageDatabase.PageCount <= 0) { return null; }

    System.Collections.Generic.List<PageData> allPages = new(PageDatabase.GetPagesByCategory(""));
    if (allPages.Count <= 0) return null;

    // Split pages by rarity
    var commonPages = new System.Collections.Generic.List<PageData>();
    var magicPages = new System.Collections.Generic.List<PageData>();
    var rarePages = new System.Collections.Generic.List<PageData>();

    foreach (PageData page in allPages) {
      if (page.Rarity == "Common") commonPages.Add(page);
      else if (page.Rarity == "Magic") magicPages.Add(page);
      else if (page.Rarity == "Rare") rarePages.Add(page);
    }

    // Roll rarity: 70% Common, 25% Magic, 5% Rare
    float rarityRoll = GD.Randf();
    if (rarityRoll < 0.70f && commonPages.Count > 0) {
      int idx = (int)(GD.Randi() % (ulong)commonPages.Count);
      return commonPages[idx];
    } else if (rarityRoll < 0.95f && magicPages.Count > 0) {
      int idx = (int)(GD.Randi() % (ulong)magicPages.Count);
      return magicPages[idx];
    } else if (rarePages.Count > 0) {
      int idx = (int)(GD.Randi() % (ulong)rarePages.Count);
      return rarePages[idx];
    }

    // Fallback to any page if specific rarity pool is empty
    int fallbackIdx = (int)(GD.Randi() % (ulong)allPages.Count);
    return allPages[fallbackIdx];
  }
}
