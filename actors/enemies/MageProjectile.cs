using Godot;

[GlobalClass]
public partial class MageProjectile : CharacterBody3D {
  public Vector3 shotPosition;

  public bool hit;
  private Timer freeTimer;

  public HitboxComponent hitbox;

  [Export] public float speed = 8.0f;
  [Export] public float lifetime = 5.0f;
  [Export] public float damage = 35.0f;
  [Export] public float homingStrength = 1.5f;
  [Export] public float pulseSpeed = 2.0f;
  [Export] public float pulseAmount = 0.15f;

  [Signal] public delegate void HitLandedEventHandler(float damage, Vector3 hitPoint);

  public Node3D target;
  public Vector3 currentDirection;
  private float baseScale;
  private float timeAlive;
  private OmniLight3D pointLight;
  private GpuParticles3D trailParticles;
  private GpuParticles3D emberParticles;
  private MeshInstance3D coreMesh;
  private MeshInstance3D outerMesh;

  public override void _Ready() {
    freeTimer = new Timer();
    AddChild(freeTimer);
    freeTimer.OneShot = true;
    freeTimer.Timeout += QueueFree;
    freeTimer.Start(lifetime);

    CollisionLayer = 0;
    CollisionMask = (uint)(CollisionLayerEnum.WORLD | CollisionLayerEnum.PLAYER);

    currentDirection = -GlobalBasis.Z;
    target = GetTree().GetFirstNodeInGroup("player") as Node3D;
    baseScale = Scale.X;

    CreateProjectileVisuals();
  }

  private void CreateProjectileVisuals() {
    StandardMaterial3D coreMat = new StandardMaterial3D();
    coreMat.AlbedoColor = new Color(1.0f, 0.8f, 0.2f);
    coreMat.Emission = new Color(1.0f, 0.6f, 0.1f);
    coreMat.EmissionEnergyMultiplier = 3.0f;
    coreMat.Metallic = 0.3f;
    coreMat.Roughness = 0.2f;

    SphereMesh coreSphere = new SphereMesh();
    coreSphere.Radius = 0.3f;
    coreSphere.Height = 0.6f;
    coreSphere.Material = coreMat;

    coreMesh = new MeshInstance3D();
    coreMesh.Mesh = coreSphere;
    AddChild(coreMesh);

    StandardMaterial3D outerMat = new StandardMaterial3D();
    outerMat.AlbedoColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    outerMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

    SphereMesh outerSphere = new SphereMesh();
    outerSphere.Radius = 0.5f;
    outerSphere.Height = 1.0f;
    outerSphere.Material = outerMat;

    outerMesh = new MeshInstance3D();
    outerMesh.Mesh = outerSphere;
    AddChild(outerMesh);

    pointLight = new OmniLight3D();
    pointLight.LightColor = new Color(1.0f, 0.7f, 0.3f);
    pointLight.LightEnergy = 1.5f;
    pointLight.OmniRange = 4.0f;
    AddChild(pointLight);

    ParticleProcessMaterial emberMat = new ParticleProcessMaterial();
    emberMat.Direction = new Vector3(0, 1, 0);
    emberMat.Spread = 45.0f;
    emberMat.InitialVelocityMin = 1.0f;
    emberMat.InitialVelocityMax = 3.0f;
    emberMat.Gravity = new Vector3(0, 2.0f, 0);
    emberMat.Color = new Color(1.0f, 0.5f, 0.0f);

    emberParticles = new GpuParticles3D();
    emberParticles.Amount = 20;
    emberParticles.Lifetime = 1.5f;
    emberParticles.ProcessMaterial = emberMat;
    emberParticles.DrawPass1 = new QuadMesh() { Size = new Vector2(0.1f, 0.1f) };
    emberParticles.Emitting = true;
    AddChild(emberParticles);

    ParticleProcessMaterial trailMat = new ParticleProcessMaterial();
    trailMat.Direction = new Vector3(0, 0, 1);
    trailMat.Spread = 10.0f;
    trailMat.InitialVelocityMin = 0.5f;
    trailMat.InitialVelocityMax = 1.5f;
    trailMat.Gravity = new Vector3(0, 0, 0);
    trailMat.Color = new Color(1.0f, 0.4f, 0.1f, 0.5f);
    trailMat.TurbulenceEnabled = true;

    trailParticles = new GpuParticles3D();
    trailParticles.Amount = 30;
    trailParticles.Lifetime = 2.0f;
    trailParticles.ProcessMaterial = trailMat;
    trailParticles.DrawPass1 = new QuadMesh() { Size = new Vector2(0.2f, 0.2f) };
    trailParticles.Emitting = true;
    trailParticles.Position = new Vector3(0, 0, 0.5f);
    AddChild(trailParticles);

    SphereShape3D shape = new SphereShape3D();
    shape.Radius = 0.5f;

    CollisionShape3D collisionShape = new CollisionShape3D();
    collisionShape.Shape = shape;
    AddChild(collisionShape);

    HitboxComponent hitboxComp = new HitboxComponent();
    hitboxComp.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HITBOX;
    hitboxComp.CollisionMask = (uint)CollisionLayerEnum.PLAYER_HURTBOX;
    hitboxComp.Monitoring = true;
    hitboxComp.Monitorable = true;
    AddChild(hitboxComp);
    hitbox = hitboxComp;

    GD.Print("MageProjectile: hitboxComp layers=", hitboxComp.CollisionLayer, " mask=", hitboxComp.CollisionMask);
  }

  public override void _PhysicsProcess(double delta) {
    if (hit) return;

    timeAlive += (float)delta;

    float pulse = 1.0f + Mathf.Sin(timeAlive * pulseSpeed) * pulseAmount;
    Scale = new Vector3(baseScale * pulse, baseScale * pulse, baseScale * pulse);

    if (pointLight != null) {
      pointLight.LightEnergy = 1.5f + Mathf.Sin(timeAlive * 10.0f) * 0.3f + Mathf.Sin(timeAlive * 23.0f) * 0.2f;
    }

    if (outerMesh != null) {
      outerMesh.Rotation = new Vector3(
        Mathf.Sin(timeAlive * 3.0f) * 0.1f,
        Mathf.Cos(timeAlive * 2.5f) * 0.1f,
        0.0f
      );
    }

    Vector3 moveDirection = currentDirection;

    if (target != null && target is PhysicsBody3D) {
      Vector3 targetPosition = target.GlobalPosition;
      targetPosition.Y += 1.0f;
      Vector3 toTarget = (targetPosition - GlobalPosition).Normalized();
      float turnAmount = Mathf.Clamp(homingStrength * 0.6f, 0.3f, 0.9f);
      moveDirection = currentDirection.Slerp(toTarget, turnAmount).Normalized();
      currentDirection = moveDirection;
    }

    KinematicCollision3D collision3D = MoveAndCollide(moveDirection * speed * (float)delta);

    if (collision3D != null) {
      hit = true;
      GD.Print("MageProjectile: collided with ", (collision3D.GetCollider() as Node)?.Name ?? "null");

      Node3D hitEffect = GetNodeOrNull<Node3D>("ProjectileHit");
      if (hitEffect != null) {
        foreach (Node child in hitEffect.GetChildren()) {
          if (child is GpuParticles3D particle) particle.Emitting = true;
        }
      }

      if (collision3D.GetCollider() is PhysicsBody3D body) {
        GetParent().RemoveChild(this);
        body.AddChild(this);
        TopLevel = false;
      } else if (hitbox != null) {
        hitbox.DisableCollisionShapes();
      }

      GD.Print("MageProjectile: HitLanded damage=", damage, " pos=", GlobalPosition);
      EmitSignal(nameof(HitLanded), damage, GlobalPosition);
      QueueFree();
    }
  }
}