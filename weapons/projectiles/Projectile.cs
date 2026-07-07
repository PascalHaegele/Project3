using Godot;

[GlobalClass]
public partial class Projectile : RigidBody3D {
  public Vector3 shotPosition = new();

  [Export] private ProjectileInfo info;

  private bool hit;
  private Timer freeTimer;

  private Weapon weapon;
  private HitboxComponent hitbox;

  private Vector3 hitPosition;

  [Signal] public delegate void HitEventHandler(Area3D area);

  public override void _Ready() {
    freeTimer = new();
    AddChild(freeTimer);
    freeTimer.OneShot = true;
    freeTimer.Timeout += QueueFree;

    weapon = GetParent<Weapon>();
    hitbox = GetNode<HitboxComponent>("HitboxComponent");
    hitbox.damage = info.damage * weapon.DamageMultiplier;
  }

  public override void _Process(double delta) {
    if(hit) { Debug.draw.DrawLine(shotPosition, hitPosition, Colors.Red); }
    else { Debug.draw.DrawLine(shotPosition, GlobalPosition, Colors.Red); }
  }

  public override void _PhysicsProcess(double delta) {
    if(hit) { return; }
    KinematicCollision3D collision3D =
      MoveAndCollide(-GlobalBasis.Z * info.speed * (float)delta);

    if(collision3D != null) {
      hit = true;
      hitPosition = GlobalPosition;
      Freeze = true;
      freeTimer.Start(3.0);

      if(collision3D.GetCollider() is PhysicsBody3D body) {
        weapon.RemoveChild(this);
        body.AddChild(this);
        TopLevel = false;
      } else {
        hitbox.DisableCollisionShapes();
      }
    }

    if(GlobalPosition.DistanceTo(shotPosition) > info.range) { QueueFree(); }
  }
}

