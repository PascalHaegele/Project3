using Godot;

[GlobalClass]
public partial class HurtboxComponent : Area3D {
  public override void _Ready() {
    SetDeferred(Area3D.PropertyName.Monitoring, false);
  }

  public void RecieveHit(HitInfo hitInfo) {
    if(GetParent() is IHitable hitable) { hitable.RecieveHit(hitInfo); }
  }
}

public interface IHitable {
  void RecieveHit(HitInfo hitInfo);
}

