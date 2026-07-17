using Godot;

public abstract partial class ActorState : State {
  public int soundLevel;

  protected Actor actor;
  protected VelocityComponent velocityComponent;
  protected AnimationPlayer? animationPlayer;

  public ActorState(Actor actor, StateMachine stateMachine) {
    this.actor = actor;
    this.stateMachine = stateMachine;
    velocityComponent = actor.GetComponent<VelocityComponent>();
    animationPlayer = actor.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
  }
}

public partial class ActorStateIdle : ActorState {
  public ActorStateIdle(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) { }

  public override void Start() => soundLevel = 0;

  public override void Enter() {
    base.Enter();

    actor.velocityInfo.Speed = 0.0f;
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.Decelerate();
  }
}

public partial class ActorStateWalk : ActorState {
  public ActorStateWalk(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) { }

  public override void Start() => soundLevel = 3;

  public override void Enter() {
    base.Enter();

    actor.velocityInfo.Speed = actor.velocityInfo.walkSpeed;
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.AccelerateInDirection(actor.Direction);
  }
}

public partial class ActorStateSprint : ActorState {
  public ActorStateSprint(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) { }

  public override void Start() => soundLevel = 5;

  public override void Enter() {
    base.Enter();

    actor.velocityInfo.Speed = actor.velocityInfo.sprintSpeed;
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.AccelerateInDirection(actor.Direction);
  }
}

public partial class ActorStateJump : ActorState {
  public ActorStateJump(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) { }

  public override void Start() => soundLevel = 3;

  public override void Enter() {
    base.Enter();

    actor.velocityInfo.Speed = actor.velocityInfo.airborneSpeed;
    velocityComponent.
      SetYVelocity(actor.velocityInfo.jumpVelocity);
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.AccelerateInDirection(actor.Direction);
  }
}

public partial class ActorStateFall : ActorState {
  public ActorStateFall(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) { }

  public override void Start() => soundLevel = 1;

  public override void Enter() {
    base.Enter();

    actor.velocityInfo.Speed = actor.velocityInfo.airborneSpeed;
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.AccelerateInDirection(actor.Direction);
  }
}

public partial class ActorStateLand : ActorState {
  public ActorStateLand(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) { }

  public override void Start() => soundLevel = 6;

  public override void Enter() {
    base.Enter();

    actor.velocityInfo.Speed = actor.velocityInfo.airborneSpeed;
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.AccelerateInDirection(actor.Direction);
  }
}

public partial class ActorStateDash : ActorState {
  public Timer cooldownTimer = new();
  private Vector3 direction;

  public ActorStateDash(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) { }

  public override void Start() {
    soundLevel = 6;

    cooldownTimer.OneShot = true;
    stateMachine.AddChild(cooldownTimer);
  }

  public override void Enter() {
    base.Enter();

    cooldownTimer.Start(actor.velocityInfo.dashCooldown);

    direction = actor.Direction;
    if(direction == Vector3.Zero) { direction = -actor.Basis.Z; }

    actor.velocityInfo.Speed = 1.0f;
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent
      .AccelerateInDirection(
        direction *
        (actor.velocityInfo.dashDistance / actor.velocityInfo.dashDuration)
      );
  }

  public override void Exit() => velocityComponent.Stop();
}

