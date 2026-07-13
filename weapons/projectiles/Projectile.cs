using Godot;

[GlobalClass]
public partial class Projectile : RigidBody3D {
  public Vector3 shotPosition;

  private bool hit;
  private Timer freeTimer;

  private Weapon weapon;
  public HitboxComponent hitbox;

  private Vector3 hitPosition;

  public override void _Ready() {
    // BodyEntered += OnBodyEntered;

    freeTimer = new();
    AddChild(freeTimer);
    freeTimer.OneShot = true;
    freeTimer.Timeout += QueueFree;

    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask =
      (uint)CollisionLayerEnum.WORLD | (uint)CollisionLayerEnum.ENEMY;

    weapon = GetParent<Weapon>();
    hitbox = GetNode<HitboxComponent>("HitboxComponent");
    hitbox.damage = weapon.info.projectileDamage * weapon.info.damageMulitplier;

    if(weapon.GetParent() is Player) {
      hitbox.CollisionLayer = (uint)CollisionLayerEnum.PLAYER_HITBOX;
      hitbox.CollisionMask = (uint)CollisionLayerEnum.ENEMY_HURTBOX;
    }

    // ApplyImpulse(-GlobalBasis.Z * weapon.info.projectileSpeed);
  }

  public override void _Process(double delta) {
    if(hit) {
      Debug
        .draw
        .DrawLineThick(shotPosition, hitPosition, 1, false, Colors.Red);
    }
    else {
      Debug
        .draw
        .DrawLineThick(shotPosition, GlobalPosition, 1, false, Colors.Red);
    }
  }

  public override void _PhysicsProcess(double delta) {
    if(hit) { return; }

    KinematicCollision3D collision3D =
      MoveAndCollide(
        -GlobalBasis.Z * weapon.info.projectileSpeed * (float)delta
      );

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

  // private void OnBodyEntered(Node body) {
  //   hit = true;
  //   hitPosition = GlobalPosition;
  //   Freeze = true;
  //   freeTimer.Start(5.0);
  //
  //   if(body is PhysicsBody3D physicsBody) {
  //     _ = weapon.CallDeferred(Node.MethodName.RemoveChild, this);
  //     _ = physicsBody.CallDeferred(Node.MethodName.AddChild, this);
  //     Owner = physicsBody.GetTree().EditedSceneRoot;
  //     TopLevel = false;
  //   } else { hitbox.DisableCollisionShapes(); }
  // }
}

