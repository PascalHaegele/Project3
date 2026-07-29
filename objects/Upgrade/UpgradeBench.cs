using Godot;
using System.Collections.Generic;

/// <summary>
/// Defines a possible upgrade that can be offered at the Upgrade Bench.
/// </summary>
public struct UpgradeOption {
  public string Description;
  public string EffectType; // e.g. "MaxHealth", "WeaponDamage", "MoveSpeed", etc.
  public float EffectValue;
  public CurrencyType RequiredCurrency;
  public int CurrencyCost;
}

/// <summary>
/// The Upgrade Bench interactable object.
/// The player can interact once per level/run to purchase one upgrade.
/// </summary>
public partial class UpgradeBench : StaticBody3D, IInteractable {
  private bool usedThisRun = false;
  private bool isOpen = false;
  private UpgradeBenchUI uiInstance;

  // Static flag that persists across bench instances in the same run
  private static bool benchUsedGlobally = false;

  public override void _Ready() {
    // Find or create the UI
    uiInstance = GetNodeOrNull<UpgradeBenchUI>("/root/UpgradeBenchUI");
    if (uiInstance == null) {
      uiInstance = new UpgradeBenchUI();
      uiInstance.Name = "UpgradeBenchUI";
      GetTree().Root.AddChild(uiInstance);
    }
    uiInstance.Visible = false;
  }

  public void Interact(Player player) {
    if (usedThisRun || benchUsedGlobally) {
      GD.Print("UpgradeBench: Already used this run.");
      return;
    }
    if (isOpen) return;

    OpenBench(player);
  }

  private void OpenBench(Player player) {
    if (uiInstance == null) return;

    // Generate 3 random upgrades
    List<UpgradeOption> upgrades = GenerateUpgrades();

    isOpen = true;
    uiInstance.ShowUI(player, upgrades, this);
    Input.MouseMode = Input.MouseModeEnum.Visible;
  }

  /// <summary>
  /// Called by the UI when the player purchases an upgrade.
  /// </summary>
  public void OnUpgradePurchased(UpgradeOption chosen, Player player) {
    ApplyUpgrade(chosen, player);
    usedThisRun = true;
    benchUsedGlobally = true;
    
    // Disable interaction permanently for this run
    GetNode<CollisionShape3D>("InteractionComponent/CollisionShape3D").Disabled = true;
    
    CloseBench();
  }

  /// <summary>
  /// Called by the UI when the player closes without purchasing.
  /// Allow reopening until purchase is made.
  /// </summary>
  public void OnBenchClosed() {
    isOpen = false;
    CloseBench();
  }

  private void CloseBench() {
    isOpen = false;
    if (uiInstance != null) {
      uiInstance.Visible = false;
    }
    Input.MouseMode = Input.MouseModeEnum.Captured;
    
    // Re-enable interaction so player can open bench again without leaving area
    // (only if not used this run)
    if (!usedThisRun && !benchUsedGlobally) {
      CollisionShape3D interactionShape = GetNodeOrNull<CollisionShape3D>("InteractionComponent/CollisionShape3D");
      if (interactionShape != null) {
        interactionShape.Disabled = false;
      }
    }
  }

  /// <summary>
  /// Resets the bench for a new level/run.
  /// </summary>
  public static void ResetAllBenches() {
    benchUsedGlobally = false;
  }

  /// <summary>
  /// Resets this specific bench instance for a new level/run.
  /// Called by EventManager or manually.
  /// </summary>
  public void ResetBenchInstance() {
    usedThisRun = false;
    isOpen = false;
    
    // Re-enable interaction collision
    CollisionShape3D interactionShape = GetNodeOrNull<CollisionShape3D>("InteractionComponent/CollisionShape3D");
    if (interactionShape != null) {
      interactionShape.Disabled = false;
    }
    
    if (uiInstance != null) {
      uiInstance.Visible = false;
    }
  }

  private void ApplyUpgrade(UpgradeOption upgrade, Player player) {
    // Deduct currency
    ItemType currencyType = upgrade.RequiredCurrency switch {
      CurrencyType.EchoFragment => ItemType.CURRENCY1,
      CurrencyType.AncientGlyph => ItemType.CURRENCY2,
      CurrencyType.ForbiddenEssence => ItemType.CURRENCY3,
      _ => ItemType.CURRENCY1
    };

    InventoryComponent inv = player.GetComponent<InventoryComponent>();
    if (inv != null) {
      inv.RemoveItem(currencyType, upgrade.CurrencyCost);
    }

    // Apply the upgrade effect
    HealthComponent health = player.GetComponent<HealthComponent>();
    switch (upgrade.EffectType) {
      case "MaxHealth":
        if (health != null) {
          health.maxHealth += (int)upgrade.EffectValue;
          health.Heal(upgrade.EffectValue);
        }
        break;
      case "WeaponDamage":
        // Store in a simple upgrade tracker on the player
        UpgradeTracker tracker = GetOrCreateTracker(player);
        tracker.weaponDamageBonus += upgrade.EffectValue;
        break;
      case "MoveSpeed":
        if (player.velocityInfo != null) {
          player.velocityInfo.multiplier += upgrade.EffectValue / 100f;
        }
        break;
      case "ReloadSpeed":
        UpgradeTracker rt = GetOrCreateTracker(player);
        rt.reloadSpeedBonus += upgrade.EffectValue;
        break;
      case "PotionCapacity":
        if (inv != null) {
          inv.maxItems[(int)ItemType.POTION] += (int)upgrade.EffectValue;
        }
        break;
      case "UpgradeRandomPage":
        UpgradeTracker pt = GetOrCreateTracker(player);
        pt.randomPageUpgrades += (int)upgrade.EffectValue;
        break;
      case "UnlockWeaponSocket":
        SocketComponent socket = player.GetComponent<SocketComponent>();
        if (socket != null) {
          socket.AddSocketSlot("Weapon");
        }
        break;
      case "UnlockArmorSocket":
        SocketComponent socketA = player.GetComponent<SocketComponent>();
        if (socketA != null) {
          socketA.AddSocketSlot("Armor");
        }
        break;
      case "UnlockSkillSocket":
        SocketComponent socketS = player.GetComponent<SocketComponent>();
        if (socketS != null) {
          socketS.AddSocketSlot("Skill");
        }
        break;
    }

    GD.Print($"[UpgradeBench] Applied: {upgrade.Description}");
  }

  private UpgradeTracker GetOrCreateTracker(Player player) {
    UpgradeTracker tracker = player.GetComponent<UpgradeTracker>();
    if (tracker == null) {
      tracker = new UpgradeTracker();
      tracker.Name = "UpgradeTracker";
      player.AddChild(tracker);
    }
    return tracker;
  }

  private List<UpgradeOption> GenerateUpgrades() {
    List<UpgradeOption> pool = new() {
      new UpgradeOption { Description = "+10 Max Health", EffectType = "MaxHealth", EffectValue = 10, RequiredCurrency = CurrencyType.EchoFragment, CurrencyCost = 1 },
      new UpgradeOption { Description = "+5% Weapon Damage", EffectType = "WeaponDamage", EffectValue = 5, RequiredCurrency = CurrencyType.EchoFragment, CurrencyCost = 1 },
      new UpgradeOption { Description = "+5% Movement Speed", EffectType = "MoveSpeed", EffectValue = 5, RequiredCurrency = CurrencyType.AncientGlyph, CurrencyCost = 1 },
      new UpgradeOption { Description = "+5% Reload Speed", EffectType = "ReloadSpeed", EffectValue = 5, RequiredCurrency = CurrencyType.AncientGlyph, CurrencyCost = 1 },
      new UpgradeOption { Description = "+1 Potion Capacity", EffectType = "PotionCapacity", EffectValue = 1, RequiredCurrency = CurrencyType.EchoFragment, CurrencyCost = 1 },
      new UpgradeOption { Description = "Upgrade Random Equipped Page", EffectType = "UpgradeRandomPage", EffectValue = 1, RequiredCurrency = CurrencyType.AncientGlyph, CurrencyCost = 1 },
      new UpgradeOption { Description = "Unlock Weapon Socket", EffectType = "UnlockWeaponSocket", EffectValue = 1, RequiredCurrency = CurrencyType.ForbiddenEssence, CurrencyCost = 1 },
      new UpgradeOption { Description = "Unlock Armor Socket", EffectType = "UnlockArmorSocket", EffectValue = 1, RequiredCurrency = CurrencyType.ForbiddenEssence, CurrencyCost = 1 },
      new UpgradeOption { Description = "Unlock Skill Socket", EffectType = "UnlockSkillSocket", EffectValue = 1, RequiredCurrency = CurrencyType.ForbiddenEssence, CurrencyCost = 1 },
    };

    // Pick 3 random distinct upgrades
    List<UpgradeOption> selected = new();
    var rng = new RandomNumberGenerator();
    rng.Randomize();

    List<int> indices = new();
    for (int i = 0; i < pool.Count; i++) indices.Add(i);

    for (int i = 0; i < 3 && indices.Count > 0; i++) {
      int pick = rng.RandiRange(0, indices.Count - 1);
      selected.Add(pool[indices[pick]]);
      indices.RemoveAt(pick);
    }

    return selected;
  }
}

/// <summary>
/// Tracks permanent upgrade bonuses on the player.
/// </summary>
[GlobalClass]
public partial class UpgradeTracker : Node {
  public float weaponDamageBonus = 0f;
  public float reloadSpeedBonus = 0f;
  public int randomPageUpgrades = 0;
}