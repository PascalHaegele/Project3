using Godot;
using System.Collections.Generic;
using FmodSharp;

public partial class Chest : StaticBody3D, IInteractable {
  private AnimationPlayer animationPlayer;
  private bool opened = false;
  private string[] availableAnimations;

  // Currency notification label (created once)
  private Label currencyNotification;

  private PackedScene potionPickup =
    GD.Load<PackedScene>("res://objects/items/potion/potion_pickup.tscn");
  private PackedScene pagePickup =
    GD.Load<PackedScene>("res://objects/items/page/page_pickup.tscn");
  private PackedScene rAmmoPickup =
    GD.Load<PackedScene>("res://objects/items/r_ammo/r_ammo_pickup.tscn");
  private PackedScene sAmmoPickup =
    GD.Load<PackedScene>("res://objects/items/s_ammo/s_ammo_pickup.tscn");
  private PackedScene currency1Pickup =
    GD.Load<PackedScene>("res://objects/items/currency/currency_1_pickup.tscn");
  private PackedScene currency2Pickup =
    GD.Load<PackedScene>("res://objects/items/currency/currency_2_pickup.tscn");
  private PackedScene currency3Pickup =
    GD.Load<PackedScene>("res://objects/items/currency/currency_3_pickup.tscn");

  [Export] private Node3D model;

  public override void _Ready() {
    // Find AnimationPlayer from the imported GLB model
    if(model.HasNode("AnimationPlayer")) {
      animationPlayer = model.GetNode<AnimationPlayer>("AnimationPlayer");
    } else {
      // Fallback: search recursively
      animationPlayer = GetAnimationPlayerRecursive(model);
    }

    if(animationPlayer != null) {
      availableAnimations = animationPlayer.GetAnimationList();
    }

    // Create floating notification label (hidden by default)
    currencyNotification = new Label();
    currencyNotification.Name = "CurrencyNotification";
    currencyNotification.Visible = false;
    currencyNotification.HorizontalAlignment = HorizontalAlignment.Center;
    AddChild(currencyNotification);
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
    FmodServerWrapper.PlayOneShotAttached("event:/Chest_Interaction_Action", this);
    if(animationPlayer == null) {
    } else {
      string animationName = FindBestAnimation();
      if(!string.IsNullOrEmpty(animationName)) {
        animationPlayer.Play(animationName);
      }
    }

    // Check insanity for currency drop BEFORE normal loot
    InsanityComponent insanity = player.GetComponent<InsanityComponent>();
    if (insanity != null) {
      if (insanity.CurrentInsanity >= insanity.HighThreshold) {
        // Drop ONE random currency
        DropCurrency(player, insanity);
      } else {
        ShowNotification("Insanity too low.");
      }
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

    // --- GUARANTEED LOOT: 1 Ammo Pack (50/50 Revolver or Shotgun) ---
    string guaranteedAmmoType = rng.Randf() < 0.5f ? "Revolver" : "Shotgun";
    DropAmmo(guaranteedAmmoType, baseSpawnPos, rng);

    // --- RANDOM LOOT ---

    // 60% chance: Drop 1 Health Potion
    if (rng.Randf() < 0.6f) {
      DropItem(ItemType.POTION, null, baseSpawnPos, rng);
    }

    // 45% chance: Drop 1 Page
    if (rng.Randf() < 0.45f) {
      PageData page = GeneratePageDrop();
      DropItem(ItemType.PAGE, page, baseSpawnPos, rng);
    }

    // 20% chance: Drop an extra Ammo Pack (random type)
    if (rng.Randf() < 0.2f) {
      string extraAmmoType = rng.Randf() < 0.5f ? "Revolver" : "Shotgun";
      DropAmmo(extraAmmoType, baseSpawnPos, rng);
    }
  }

  private void DropCurrency(Player player, InsanityComponent insanity) {
    // Pick one random currency
    var rng = new RandomNumberGenerator();
    rng.Randomize();

    int currencyIndex = rng.RandiRange(0, 2);
    ItemType currencyType = currencyIndex switch {
      0 => ItemType.CURRENCY1,
      1 => ItemType.CURRENCY2,
      _ => ItemType.CURRENCY3
    };

    PackedScene pickupScene = currencyIndex switch {
      0 => currency1Pickup,
      1 => currency2Pickup,
      _ => currency3Pickup
    };

    string currencyName = currencyIndex switch {
      0 => "Echo Fragment",
      1 => "Ancient Glyph",
      _ => "Forbidden Essence"
    };

    // Spawn the currency pickup at LootSpawn
    Marker3D lootSpawn = GetNodeOrNull<Marker3D>("LootSpawn");
    if (lootSpawn != null) {
      Pickup currencyPickup = pickupScene.Instantiate<Pickup>();
      AddChild(currencyPickup);
      currencyPickup.GlobalPosition = lootSpawn.GlobalPosition;
      // currencyPickup.itemType = currencyType;
      currencyPickup.amount = 1;
      currencyPickup.ApplyImpulse(GetImpulseVector());
    }

    // Show floating notification
    ShowNotification($"+1 {currencyName}");
  }

  private void ShowNotification(string message) {
    if (currencyNotification == null) return;

    currencyNotification.Text = message;
    currencyNotification.Visible = true;

    // Create a tween to fade out after 2 seconds
    Tween tween = CreateTween();
    tween.TweenInterval(1.5f);
    tween.TweenProperty(currencyNotification, "modulate:a", 0.0f, 0.5f);
    tween.TweenCallback(Callable.From(() => {
      currencyNotification.Visible = false;
      currencyNotification.Modulate = new Color(1, 1, 1, 1);
    }));
  }

  /// <summary>
  /// Drops an ammo pickup with the correct model visibility.
  /// </summary>
  private void DropAmmo(string ammoType, Vector3 baseSpawnPos, RandomNumberGenerator rng) {
    PackedScene pickupScene = ammoType switch {
      "Shotgun" => sAmmoPickup,
      _ => rAmmoPickup
    };
    Pickup pickupInstance = pickupScene.Instantiate<Pickup>();
    AddChild(pickupInstance);

    Vector3 pos = baseSpawnPos;
    pos.X += (float)GD.RandRange(-0.4, 0.4);
    pos.Z += (float)GD.RandRange(-0.4, 0.4);
    pickupInstance.GlobalPosition = pos;

    pickupInstance.pageData = null;
    if (ammoType == "Shotgun") {
      // pickupInstance.itemType = ItemType.S_AMMO;
      pickupInstance.amount = 12;
    } else {
      // pickupInstance.itemType = ItemType.R_AMMO;
      pickupInstance.amount = 18;
    }

    // Use same impulse as original chest
    pickupInstance.ApplyImpulse(GetImpulseVector());
  }

  /// <summary>
  /// Drops a general item (potion or page) using the same spawn pattern as the original chest.
  /// </summary>
  private void DropItem(ItemType type, PageData page, Vector3 baseSpawnPos, RandomNumberGenerator rng) {
    PackedScene pickupScene = type switch {
      ItemType.POTION => potionPickup,
      _ => pagePickup
    };

    Pickup pickupInstance = pickupScene.Instantiate<Pickup>();

    AddChild(pickupInstance);

    Vector3 pos = baseSpawnPos;
    pos.X += (float)GD.RandRange(-0.4, 0.4);
    pos.Z += (float)GD.RandRange(-0.4, 0.4);
    pickupInstance.GlobalPosition = pos;

    // pickupInstance.itemType = type;
    pickupInstance.pageData = page;

    // Use same impulse as original chest
    pickupInstance.ApplyImpulse(GetImpulseVector());
  }

  private Vector3 GetImpulseVector() {
    Vector3 impulse = new(
      (float)GD.RandRange(-0.3, 0.3),
      3.0f,
      (float)GD.RandRange(-0.3, 0.3) + 3.0f
    );
    impulse = impulse.Rotated(Vector3.Up, GlobalRotation.Y);
    return impulse;
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

    List<PageData> allPages = new(PageDatabase.GetPagesByCategory(""));
    if (allPages.Count <= 0) return null;

    // Split pages by rarity
    var commonPages = new List<PageData>();
    var magicPages = new List<PageData>();
    var rarePages = new List<PageData>();

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
