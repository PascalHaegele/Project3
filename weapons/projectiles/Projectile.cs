using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class Projectile : RigidBody3D {
  public Vector3 shotPosition;

  private bool hit;
  private Timer freeTimer;

  private Weapon weapon;
  public HitboxComponent hitbox;

  private Vector3 hitPosition;

  // Empowered shot flag
  public bool isEmpowered = false;
  private bool isHoming;
  private float homingStrength;
  private float homingRange = 12.0f;
  private Vector3 currentDirection;

  public override void _Ready() {
    freeTimer = new();
    AddChild(freeTimer);
    freeTimer.OneShot = true;
    freeTimer.Timeout += QueueFree;

    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask =
      (uint)CollisionLayerEnum.WORLD |
      (uint)CollisionLayerEnum.ENEMY;

    weapon = GetParent<Weapon>();
    hitbox = GetNode<HitboxComponent>("HitboxComponent");

    // Initialize current direction to forward so homing starts smoothly
    currentDirection = -GlobalBasis.Z;

    if (weapon.GetParent() is Player) {
      hitbox.CollisionLayer = (uint)CollisionLayerEnum.PLAYER_HITBOX;
      hitbox.CollisionMask = (uint)CollisionLayerEnum.ENEMY_HURTBOX;
    }
  }

  /// <summary>
  /// Recalculate damage now that isEmpowered and socket modifiers are finalized.
  /// </summary>
  public void RecalculateDamage() {
    float baseDamage = weapon.info.projectileDamage * weapon.info.damageMulitplier;
    Player player = weapon.GetParent() as Player ?? weapon.GetParent()?.GetParent<Player>();
    if (player != null) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if (socket != null) {
        baseDamage += socket.GetModifier("Damage");
        if (socket.HasModifier("HomingProjectiles")) {
          isHoming = true;
          homingStrength = socket.GetModifier("HomingProjectiles");
          GD.Print($">>> DEBUG HomingProjectiles: strength={homingStrength}, range={homingRange}");
        }
      }
    }

    hitbox.damage = baseDamage;
    if (isEmpowered) {
      hitbox.damage *= 2.0f;
    }
    GD.Print($">>> DEBUG Projectile.RecalculateDamage: empowered={isEmpowered}, baseDamage={baseDamage:F1}, finalDamage={hitbox.damage:F1}");
  }

  public override void _Process(double delta) {
    if(hit) {
      Debug.draw.DrawLineThick(shotPosition, hitPosition, 1, false, Colors.Red);
    } else {
      Debug.draw.DrawLineThick(shotPosition, GlobalPosition, 1, false, Colors.Red);
    }
  }

  private Node3D? FindNearestTarget() {
    Node3D? nearestTarget = null;
    float bestDistance = homingRange;

    // Use Godot's built-in group system for reliable enemy detection
    Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");

    Node3D? shooter = weapon?.GetParent() as Node3D;

    foreach(Node candidate in enemies) {
      if(candidate is not Node3D target) { continue; }
      if(shooter != null && target == shooter) { continue; }

      float distance = GlobalPosition.DistanceTo(target.GlobalPosition);
      if(distance <= bestDistance) {
        bestDistance = distance;
        nearestTarget = target;
      }
    }

    if(nearestTarget != null) {
      GD.Print($">>> DEBUG Homing: Found target at distance {bestDistance:F2}");
    }

    return nearestTarget;
  }

  public override void _PhysicsProcess(double delta) {
    if(hit) { return; }

    Vector3 moveDirection = currentDirection;

    if(isHoming && homingStrength > 0.0f) {
      Node3D? target = FindNearestTarget();
      if(target != null) {
        Vector3 toTarget = (target.GlobalPosition - GlobalPosition).Normalized();
        // Higher turn rate for reliable hits (cyberpunk-style tracking)
        float turnAmount = Mathf.Clamp(homingStrength * 0.6f, 0.3f, 0.9f);
        moveDirection = moveDirection.Slerp(toTarget, turnAmount).Normalized();
        currentDirection = moveDirection;
      }
    }

    KinematicCollision3D collision3D =
      MoveAndCollide(moveDirection * weapon.info.projectileSpeed * (float)delta);

    if(collision3D != null) {
      hit = true;
      hitPosition = GlobalPosition;
      Freeze = true;
      freeTimer.Start(5.0);

      if(collision3D.GetCollider() is PhysicsBody3D body) {
        weapon.RemoveChild(this);
        body.AddChild(this);
        TopLevel = false;
      } else { hitbox.DisableCollisionShapes(); }
    }

    if(GlobalPosition.DistanceTo(shotPosition) > weapon.info.range) {
      QueueFree();
    }
  }
}