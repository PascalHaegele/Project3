using Godot;

public struct HitInfo {
  public float damage;
  public Vector3 direction;
}

[GlobalClass]
public partial class HitboxComponent : Area3D {
  public float damage;

  public double? lifetime;
  public Shape3D? shape;
  public HitLog? hitLog;

  public override void _Ready() {
    SetDeferred(Area3D.PropertyName.Monitorable, false);
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

  public void EnableCollisionShapes() {
    for(int i = 0; i < GetChildCount(); i++) {
      if(GetChildOrNull<CollisionShape3D>(i) != null) {
        GetChild<CollisionShape3D>(i).Disabled = false;
      }
    }
  }

  private void OnAreaEntered(Area3D area) {
    if(area is HurtboxComponent hurtbox) {
      HitInfo info = new();
      info.damage = damage;
      if(GetParent() is Projectile p) {
        info.direction = GlobalPosition.DirectionTo(p.shotPosition);
      }

      if(hitLog != null) {
        Node hurtboxOwner = hurtbox.GetParent();
        if(hitLog.HasHit(hurtboxOwner)) { return; }
        else { hitLog.LogHit(hurtboxOwner); }
      }

      hurtbox.RecieveHit(info);
    }
    if(GetParent() is Projectile) { DisableCollisionShapes(); }
  }
}

