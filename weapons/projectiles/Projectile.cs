using Godot;

[GlobalClass]
public partial class Projectile : RigidBody3D {
  [Export] private ProjectileInfo info;

  private bool hit;
  private Timer freeTimer;

  private Weapon owner;
  private HitboxComponent hitbox;

  private Vector3 start;

  [Signal] public delegate void HitEventHandler(Area3D area);

  public override void _Ready() {
    freeTimer = new();
    AddChild(freeTimer);
    freeTimer.OneShot = true;
    freeTimer.Timeout += QueueFree;

    owner = GetParent<Weapon>();
    hitbox = GetNode<HitboxComponent>("HitboxComponent");
    hitbox.damage = info.damage * owner.DamageMultiplier;

    start = GlobalPosition;
  }

  public override void _Process(double delta) {
    Debug.draw.DrawLine(start, GlobalPosition, Colors.Red);
  }

  public override void _PhysicsProcess(double delta) {
    if(hit) { return; }
    KinematicCollision3D collision3D =
      MoveAndCollide(-GlobalBasis.Z * info.speed * (float)delta);

    if(collision3D != null) {
      hit = true;
      Freeze = true;
      freeTimer.Start(3.0);

      if(collision3D.GetCollider() is PhysicsBody3D body) {
        owner.RemoveChild(this);
        body.AddChild(this);
        TopLevel = false;
      }
    }

    if(GlobalPosition.DistanceTo(start) > info.range) { QueueFree(); }
  }
}
