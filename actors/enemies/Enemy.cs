using Godot;

public struct AIInfo {
  public bool hasTarget;
  public bool soundHeard;
  public bool playerVisible;
  public bool beeingShot;

  public Vector3 targetPosition;
  public Vector3 soundPosition;
  public Vector3 shotFromDirection;
}

[GlobalClass]
public abstract partial class Enemy : Actor {
  [Export] public EnemyInfo enemyInfo;
  public AIInfo aiInfo;
  public EnemyDifficultyInfo difficultyInfo;

  public float LeashDistance => GlobalPosition.DistanceTo(enemyInfo.leashPoint);
  public bool InsideLeashLength => LeashDistance <= enemyInfo.leashLength;

  public AnimationPlayer animationPlayer;

  protected VelocityComponent velocityComponent;
  protected HealthComponent healthComponent;

  protected HurtboxComponent hurtboxComponent;

  [Export] private ShaderMaterial dissolveMaterial;
  [Export] private ShaderMaterial outlineMaterial;
  [Export] private Node3D model;

  public bool TargetInRange  {
    get {
      float distance =  GlobalPosition.DistanceTo(aiInfo.targetPosition);
      return
        distance > enemyInfo.minAttackRange &&
        distance < enemyInfo.maxAttackRange;
    }
  }

  public float DistanceToTarget =>
    GlobalPosition.DistanceTo(aiInfo.targetPosition);

  [Signal] public delegate void KilledEventHandler(Enemy enemy);

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.ENEMY;
    CollisionMask =
      (uint)CollisionLayerEnum.WORLD |
      (uint)CollisionLayerEnum.PLAYER |
      (uint)CollisionLayerEnum.ENEMY;

    difficultyInfo.Changed += ApplyDifficulty;

    animationPlayer = GetComponent<AnimationPlayer>();

    velocityComponent = GetComponent<VelocityComponent>();

    healthComponent = GetComponent<HealthComponent>();
    healthComponent.Died += OnDeath;

    hurtboxComponent = GetComponent<HurtboxComponent>();
    hurtboxComponent.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HURTBOX;
    hurtboxComponent.CollisionMask = (uint)CollisionLayerEnum.NONE;

    // Add to "enemies" group for homing projectile targeting
    AddToGroup("enemies");

    foreach(Node child in model.GetChildren()) {
      if(child is MeshInstance3D mesh) {
        if(mesh.Mesh is ArrayMesh arrayMesh) {
          Material mat = arrayMesh.SurfaceGetMaterial(0);
          mat.NextPass = outlineMaterial;
        }
        if(mesh.Mesh is PrimitiveMesh primitiveMesh) {
          primitiveMesh.Material ??= new StandardMaterial3D();
          primitiveMesh.Material.NextPass = outlineMaterial;
        }
      }
    }
  }

  protected virtual void ApplyDifficulty() { }

  private async void OnDeath() {
    if(dissolveMaterial != null && model != null) {
      Tween tween = CreateTween();
      Timer timer = new();
      timer.OneShot = true;
      AddChild(timer);
      timer.Start(3.0);

      foreach(Node child in model.GetChildren()) {
        if(child is MeshInstance3D mesh) {
          mesh.MaterialOverride = dissolveMaterial;
          ShaderMaterial meshShader = mesh.MaterialOverride as ShaderMaterial;
          meshShader.SetShaderParameter("t", 0.0);

          _ = tween.TweenMethod(
            Callable.From(
              (float value) => meshShader.SetShaderParameter("t", value)
            ),
            0.0,
            1.0,
            3.0
          );
        }
      }

      // _ = await ToSignal(tween, Tween.SignalName.Finished);
      _ = await ToSignal(timer, Timer.SignalName.Timeout);
    }

    QueueFree();
  }
}

