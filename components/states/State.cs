using Godot;

[GlobalClass]
public abstract partial class State : Resource {
  public VelocityStats actorVelocityStats;
  public InputPackage input = new();

  protected StateMachine stateMachine;

  [Signal]
  public delegate void TransitionEventHandler(State newState);

  public abstract void CheckRelevance();

  public virtual void Init(
    VelocityStats actorVelocityStats,
    StateMachine targetStateMachine,
    bool local = true
  ) {
    this.actorVelocityStats = actorVelocityStats;
    stateMachine = targetStateMachine;
    ResourceLocalToScene = local;
  }

  public virtual void Update(double delta) { }

  public virtual void PhysicsUpdate(double delta) { }

  public virtual void Enter() { }

  public virtual void Exit() { }
}

