using Godot;

[GlobalClass]
public partial class HitboxComponent : Area3D {
  public float damage;

  public double? lifetime;
  public Shape3D? shape;
  public HitLog? hitLog;

  public override void _Ready() {
    Monitorable = false;
    AreaEntered += OnAreaEntered;

    if(lifetime.HasValue) {
      Timer lifetimeTimer = new();
      AddChild(lifetimeTimer);
      lifetimeTimer.Timeout += QueueFree;
      lifetimeTimer.Start(lifetime.Value);
    }

    if(shape != null) {
      CollisionShape3D collisionShape = new();
      AddChild(collisionShape);
      collisionShape.Shape = shape;
    }
  }

  public void DisableCollisionShapes() {
    for(int i = 0; i < GetChildCount(); i++) {
      if(GetChildOrNull<CollisionShape3D>(i) != null) {
        GetChild<CollisionShape3D>(i).Disabled = true;
      }
    }
  }

  private void OnAreaEntered(Area3D area) {
    if(area is HurtboxComponent hurtbox) {
      if(hitLog != null) {
        Node hurtboxOwner = hurtbox.GetParent();
        if(hitLog.HasHit(hurtboxOwner)) { return; }
        else { hitLog.LogHit(hurtboxOwner); }
      }

      hurtbox.RecieveHit(damage);
    }
    DisableCollisionShapes();
  }
}

