using Godot;

// Handles life-cycle, damage, and healing for an actor
[GlobalClass]
public partial class HealthComponent : Node {
  [Export] public float maxHealth = 100.0f;

  public float CurrentHealth { get; private set; }
  public bool IsAlive => CurrentHealth > 0.0f;

  [Signal] public delegate void DiedEventHandler();
  [Signal] public delegate void HealthChangedEventHandler(float newHealth);

  // Track active bleed effects: stack count and remaining tick timer
  private int bleedStacks;
  private float bleedTimer;
  private float bleedTickInterval = 1.0f;
  private float bleedDamagePerTick = 0.0f;

  public override void _Ready() {
    maxHealth = GetEffectiveMaxHealth();
    CurrentHealth = maxHealth;
  }

  public override void _PhysicsProcess(double delta) {
    if(!IsAlive) { return; }
    if(bleedStacks <= 0) { return; }

    bleedTimer -= (float)delta;
    if(bleedTimer <= 0.0f) {
      bleedTimer = bleedTickInterval;
      float bleedDmg = bleedDamagePerTick * bleedStacks;
      CurrentHealth = Mathf.Max(0.0f, CurrentHealth - bleedDmg);
      EmitSignalHealthChanged(CurrentHealth);
      GD.Print($"{GetParent().Name}: Bleed tick -{bleedDmg} (stacks: {bleedStacks}) | Health: {CurrentHealth}/{GetEffectiveMaxHealth()}");

      if(CurrentHealth <= 0.0f) {
        GD.Print($"{GetParent().Name} Died (Bleed)");
        EmitSignalDied();
      }
    }
  }

  public void Reset() {
    ClearBleed();
    CurrentHealth = maxHealth;
  }

  public void TakeDamage(float damage) {
    if(!IsAlive) { return; }

    // Apply DamageReduction if available
    float finalDamage = damage;
    if(GetParent() is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("DamageReduction")) {
        float reduction = socket.GetModifier("DamageReduction");
        // Value is percentage reduction (e.g. 5 = 5%)
        finalDamage = damage * (1.0f - reduction / 100.0f);
        if(!Mathf.IsEqualApprox(finalDamage, damage)) {
          GD.Print($">>> DAMAGE REDUCED by {reduction}%: {damage} -> {finalDamage}");
        }
      }
    }

    CurrentHealth = Mathf.Max(0.0f, CurrentHealth - finalDamage);

    EmitSignalHealthChanged(CurrentHealth);

    GD.Print($"Damage Taken: {finalDamage} | Health: {CurrentHealth}/{GetEffectiveMaxHealth()}");

    if(CurrentHealth <= 0.0f) {
      GD.Print($"{GetParent().Name} Died");
      EmitSignalDied();
    }
  }

  public void Heal(float amount) {
    if(!IsAlive) { return; }

    float effectiveMax = GetEffectiveMaxHealth();
    CurrentHealth = Mathf.Min(effectiveMax, CurrentHealth + amount);

    EmitSignalHealthChanged(CurrentHealth);

    GD.Print($"Healed: {amount} | Health: {CurrentHealth}/{effectiveMax}");
  }

  /// <summary>
  /// Call this when the player defeats an elite enemy or completes an event.
  /// </summary>
  public void OnEliteKill() {
    if(!IsAlive) return;
    TryApplyBloodRitual();
  }

  private void TryApplyBloodRitual() {
    if(GetParent() is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("BloodRitual")) {
        float healAmount = socket.GetModifier("BloodRitual");
        Heal(healAmount);
        GD.Print($">>> DEBUG BloodRitual: healed={healAmount} HP, current={CurrentHealth:F1}/{GetEffectiveMaxHealth():F1}");
      }
    }
  }

  /// <summary>
  /// Applies bleed: adds stacks. Timer only resets on first application.
  /// </summary>
  public void ApplyBleed(float damagePerTick, int stacks = 1) {
    bool wasEmpty = bleedStacks == 0;
    bleedStacks += stacks;
    bleedDamagePerTick = Mathf.Max(bleedDamagePerTick, damagePerTick);
    if(wasEmpty) {
      bleedTimer = bleedTickInterval; // Only reset when starting fresh
    }
    GD.Print($"{GetParent().Name}: Bleed applied! Stacks: {bleedStacks}, DPS: {bleedDamagePerTick}");
  }

  public void ClearBleed() {
    bleedStacks = 0;
    bleedDamagePerTick = 0.0f;
    bleedTimer = 0.0f;
  }

  /// <summary>
  /// Returns effective max health including socket modifiers.
  /// Only applies to Player; enemies ignore this.
  /// </summary>
  public float GetEffectiveMaxHealth() {
    float effective = maxHealth;
    if(GetParent() is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null) {
        effective += socket.GetModifier("Health");
      }
    }
    return effective;
  }
}
