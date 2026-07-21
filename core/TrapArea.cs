using Godot;

[GlobalClass]
public partial class TrapArea : Area3D {
  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask = (uint)CollisionLayerEnum.PLAYER;

    Monitorable = false;

    BodyEntered += OnBodyEntered;
  }

  private void OnBodyEntered(Node3D body) {
    foreach(Node child in GetChildren()) {
      if(child is ITrap trap) { trap.Activate(); }
    }
  }
}

public interface ITrap {
  void Activate();
}

