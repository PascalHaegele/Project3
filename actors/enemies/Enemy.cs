using Godot;

[GlobalClass]
public partial class Enemy : Actor {
  [Export] public Marker3D[]? patrolPath;
  [Export] public Marker3D? leashPoint;

  [Export] public float attackRange = 2.0f;
  [Export] public float leashLength = 20.0f;

  public bool playerInVision;

  protected AIStateMachine aiStateMachine;
  protected StateMachine stateMachine;
  protected VelocityComponent velocityComponent;
  protected HealthComponent healthComponent;

  protected Area3D visionArea;

  public override void _Ready() {
    aiStateMachine = GetComponent<AIStateMachine>();
    stateMachine = GetComponent<StateMachine>();
    velocityComponent = GetComponent<VelocityComponent>();
    healthComponent = GetComponent<HealthComponent>();

    visionArea = GetNode<Area3D>("Vision");
  }
}

