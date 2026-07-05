using Godot;

// Handles life-cycle, damage, and healing for an actor
[GlobalClass]
public partial class HealthComponent : Node {
  [Export] public float MaxHealth = 100.0f;

  // Current health value; only modifiable via TakeDamage or Heal
  public float CurrentHealth { get; private set; }

  // Returns true if health is above zero
  public bool IsAlive => CurrentHealth > 0.0f;

  // Signal emitted when health drops to zero
  [Signal] public delegate void DiedEventHandler();

  // Signal emitted whenever health changes
  [Signal] public delegate void HealthChangedEventHandler(float newHealth);

  public override void _Ready() {
    CurrentHealth = MaxHealth;
  }

  // Applies damage and checks for death conditions
  public void TakeDamage(float damage) {
    if(!IsAlive) { return; }

    CurrentHealth = Mathf.Max(0.0f, CurrentHealth - damage);

    EmitSignalHealthChanged(CurrentHealth);

    GD.Print($"Damage Taken: {damage} | Health: {CurrentHealth}/{MaxHealth}");

    if(CurrentHealth <= 0.0f) {
      GD.Print($"{GetParent().Name} Died");
      EmitSignalDied();
    }
  }
}

