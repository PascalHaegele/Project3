using Godot;

[GlobalClass]
public abstract partial class AIState : Resource {
  protected Enemy actor;
  protected Player player;
  protected AIStateMachine stateMachine;
  protected NavigationAgent3D navAgent;

  public InputPackage input = new();

  [Signal] public delegate void TransitionEventHandler(AIState newState);

  public abstract void CheckRelevance(
    float playerDistance,
    float leashDistance
  );

  public void Init(
    Enemy actor,
    Player player,
    AIStateMachine stateMachine,
    NavigationAgent3D navAgent
  ) {
    this.actor = actor;
    this.player = player;
    this.stateMachine = stateMachine;
    this.navAgent = navAgent;
  }

  public virtual void Start() { }

  public virtual void Enter() { }

  public virtual void Update(double delta) { }

  public virtual void PhysicsUpdate(double delta) { }

  public virtual void Exit() { }
}

