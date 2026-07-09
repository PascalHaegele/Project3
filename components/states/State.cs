using Godot;

[GlobalClass]
public abstract partial class State : Resource {
  protected Actor actor;
  protected StateMachine stateMachine;
  protected VelocityInfo velocityInfo;
  protected VelocityComponent velocityComponent;

  public InputPackage input = new();

  [Signal] public delegate void TransitionEventHandler(State newState);

  public abstract void CheckRelevance();

  public virtual void Init(Actor actor, StateMachine stateMachine) {
    this.actor = actor;
    this.stateMachine = stateMachine;

    velocityInfo = actor.velocityInfo;
    velocityComponent = actor.GetComponent<VelocityComponent>();
  }

  public virtual void Start() { }

  public virtual void Enter() { }

  public virtual void Update(double delta) { }

  public virtual void PhysicsUpdate(double delta) { }

  public virtual void Exit() { }
}

