using Godot;

public struct AIInfo {
  public bool hasTarget;
  public bool soundHeard;
  public bool playerVisible;
  public bool beeingShot;

  public Vector3 targetPosition;
  public Vector3 soundPosition;
  public Vector3 shotFromPosition;
}

[GlobalClass]
public abstract partial class Enemy : Actor {
  [Export] public EnemyInfo enemyInfo;
  public AIInfo aiInfo;

  public AnimationPlayer animationPlayer;

  public bool playerInVision;
  public bool hearingPlayer;

  protected AIStateMachine aiStateMachine;
  protected VelocityComponent velocityComponent;
  protected HealthComponent healthComponent;
  protected AIDetectionComponent detectionComponent;

  public override void _Ready() {
    enemyInfo.ResourceLocalToScene = true;

    CollisionLayer = (uint)CollisionLayerEnum.ENEMY;
    CollisionMask =
      (uint)CollisionLayerEnum.WORLD |
      (uint)CollisionLayerEnum.PLAYER |
      (uint)CollisionLayerEnum.ENEMY;

    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

    aiStateMachine = GetComponent<AIStateMachine>();
    velocityComponent = GetComponent<VelocityComponent>();
    healthComponent = GetComponent<HealthComponent>();
    detectionComponent = GetComponent<AIDetectionComponent>();
  }
}

