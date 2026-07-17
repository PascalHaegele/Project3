using Godot;

[GlobalClass]
public partial class HurtboxComponent : Area3D {
  public override void _Ready() {
    SetDeferred(Area3D.PropertyName.Monitoring, true);
  }

  public void RecieveHit(HitInfo hitInfo) {
    Node node = GetParent();
    while (node != null) {
      if (node is IHitable hitable) {
        hitable.RecieveHit(hitInfo);
        return;
      }
      node = node.GetParent();
    }
    GD.PrintErr("HurtboxComponent: No IHitable found in parent chain!");
  }
}

public interface IHitable {
  void RecieveHit(HitInfo hitInfo);
}

