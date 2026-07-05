using Godot;

[GlobalClass]
public partial class HurtboxComponent : Area3D {
  private HealthComponent healthComponent;

  public override void _Ready() {
    healthComponent = GetParent<Actor>().GetComponent<HealthComponent>();
    Monitoring = false;
  }

  public void RecieveHit(float damage) {
    healthComponent.TakeDamage(damage);
  }
}

