using Godot;

public partial class TestEnemy : Actor {
  private HealthComponent healthComponent;

  public override void _Ready() {
    healthComponent = GetNode<HealthComponent>("HealthComponent");
    healthComponent.Died += OnDeath;
  }

  private void OnDeath() {
    QueueFree();
  }
}

