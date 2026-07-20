using Godot;

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

    float baseDamage = weapon.info.projectileDamage * weapon.info.damageMulitplier;
    Player player = weapon.GetParent() as Player ?? weapon.GetParent()?.GetParent<Player>();
    if (player != null) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if (socket != null) {
        baseDamage += socket.GetModifier("Damage");
      }
    }

    // Apply FrenziedSoul empowered shot bonus
    if (isEmpowered) {
      baseDamage *= 2.0f;
    }

    hitbox.damage = baseDamage;

    if (weapon.GetParent() is Player) {
      hitbox.CollisionLayer = (uint)CollisionLayerEnum.PLAYER_HITBOX;
      hitbox.CollisionMask = (uint)CollisionLayerEnum.ENEMY_HURTBOX;
    }
  }

  public override void _Process(double delta) {
    if(hit) {
      Debug.draw.DrawLineThick(shotPosition, hitPosition, 1, false, Colors.Red);
    } else {
      Debug.draw.DrawLineThick(shotPosition, GlobalPosition, 1, false, Colors.Red);
    }
  }

  public override void _PhysicsProcess(double delta) {
    if(hit) { return; }

    KinematicCollision3D collision3D =
      MoveAndCollide(-GlobalBasis.Z * weapon.info.projectileSpeed * (float)delta);

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
