using Godot;

[GlobalClass]
public partial class Enemy : Actor {
  protected AIComponent aiComponent;
  protected StateMachine stateMachine;
  protected VelocityComponent velocityComponent;
  protected HealthComponent healthComponent;

  protected Area3D visionArea;

  [Export] public Marker3D[]? patrolPath;

  public override void _Ready() {
    aiComponent = GetComponent<AIComponent>();
    stateMachine = GetComponent<StateMachine>();
    velocityComponent = GetComponent<VelocityComponent>();
    healthComponent = GetComponent<HealthComponent>();

    visionArea = GetNode<Area3D>("Vision");
  }
}

