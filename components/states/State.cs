using Godot;

[GlobalClass]
public abstract partial class State : Resource {
  protected Actor actor;
  protected StateMachine stateMachine;
  protected VelocityInfo? actorVelocityInfo;
  protected VelocityComponent? actorVelocityComponent;

  public InputPackage input = new();

  [Signal] public delegate void TransitionEventHandler(State newState);

  public abstract void CheckRelevance();

  public virtual void Init(
    Actor targetActor,
    StateMachine targetStateMachine,
    VelocityInfo? targetActorVelocityInfo
  ) {
    actor = targetActor;
    stateMachine = targetStateMachine;
    actorVelocityInfo = targetActorVelocityInfo;

    actorVelocityComponent = actor.GetComponentOrNull<VelocityComponent>();
  }

  public virtual void Ready() { }

  public virtual void Enter() { }

  public virtual void Update(double delta) { }

  public virtual void PhysicsUpdate(double delta) { }

  public virtual void Exit() { }
}

