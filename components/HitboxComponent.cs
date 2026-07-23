using Godot;

public struct HitInfo {
  public float damage;
  public Vector3 direction;
  public Actor shooter;
}

  [GlobalClass]
  public partial class HitboxComponent : Area3D {
    public Actor actor;

    public float damage;

    public double? lifetime;
    public Shape3D? shape;
    public HitLog? hitLog;

    [Signal] public delegate void HitLandedEventHandler(float damage, Vector3 hitPoint);

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

  /// <summary>
  /// Applies crit multiplier. Not used in simplified system.
  /// </summary>
  public float ApplyCrit(float baseDamage) {
    return baseDamage;
  }

  /// <summary>
  /// Applies bleed DoT if owner has Bleed socketed. Only works for player-owned projectiles.
  /// </summary>
  public void ApplyBleed(HurtboxComponent hurtbox) {
    // Bleed not implemented in simplified system
  }

  private void OnAreaEntered(Area3D area) {
    if(area is HurtboxComponent hurtbox) {
      HitInfo info = new();
      info.damage = ApplyCrit(damage);
      if(GetParent() is Projectile p) {
        info.direction = GlobalPosition.DirectionTo(p.shotPosition);
      }
      info.shooter = actor;

      if(hitLog != null) {
        Node hurtboxOwner = hurtbox.GetParent();
        if(hitLog.HasHit(hurtboxOwner)) { return; }
        else { hitLog.LogHit(hurtboxOwner); }
      }

      hurtbox.RecieveHit(info);

      // Apply bleed on hit
      ApplyBleed(hurtbox);

      // Notify weapon system about hit for effects
      EmitSignalHitLanded(damage, GlobalPosition);
    }
    if(GetParent() is Projectile) { DisableCollisionShapes(); }
  }
}
