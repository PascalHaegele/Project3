using Godot;

[GlobalClass]
public partial class HurtboxComponent : Area3D {
  private Actor actor;
  private HealthComponent healthComponent;

  public override void _Ready() {
    actor = GetParent<Actor>();
    healthComponent = actor.GetComponent<HealthComponent>();
    SetDeferred(Area3D.PropertyName.Monitoring, false);
  }

  public void RecieveHit(HitInfo hitInfo) {
    healthComponent.TakeDamage(hitInfo.damage);
    if(actor is IHitable hitable) { hitable.RecieveHit(hitInfo); }
  }
}

public interface IHitable {
  void RecieveHit(HitInfo hitInfo);
}

