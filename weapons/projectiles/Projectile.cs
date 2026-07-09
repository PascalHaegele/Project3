using Godot;

[GlobalClass]
public partial class Projectile : RigidBody3D {
  public Vector3 shotPosition = new();

  private bool hit;
  private Timer freeTimer;

  private Weapon weapon;
  public HitboxComponent hitbox;

  private Vector3 hitPosition;

  [Signal] public delegate void HitEventHandler(Area3D area);

  public override void _Ready() {
    freeTimer = new();
    AddChild(freeTimer);
    freeTimer.OneShot = true;
    freeTimer.Timeout += QueueFree;

    weapon = GetParent<Weapon>();
    hitbox = GetNode<HitboxComponent>("HitboxComponent");
    hitbox.damage = weapon.info.projectileDamage * weapon.info.damageMulitplier;
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
      } else {
        hitbox.DisableCollisionShapes();
      }
    }

    if(GlobalPosition.DistanceTo(shotPosition) > weapon.info.range) {
      QueueFree();
    }
  }
}

