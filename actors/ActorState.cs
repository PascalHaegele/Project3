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

  protected float ApplyMovementSpeedModifier(float baseSpeed) {
    if(actor is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("MovementSpeed")) {
        float bonusPercent = socket.GetModifier("MovementSpeed") / 100.0f;
        float effectiveSpeed = baseSpeed * (1.0f + bonusPercent);
        GD.Print($">>> DEBUG MovementSpeed: base={baseSpeed:F2}, modifier={socket.GetModifier("MovementSpeed")}, result={effectiveSpeed:F2}");
        return effectiveSpeed;
      }
    }
    return baseSpeed;
  }

  protected float ApplyDashDistanceModifier(float baseDistance) {
    if(actor is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("DashDistance")) {
        float bonusPercent = socket.GetModifier("DashDistance") / 100.0f;
        float effectiveDistance = baseDistance * (1.0f + bonusPercent);
        GD.Print($">>> DEBUG DashDistance: base={baseDistance:F2}, modifier={socket.GetModifier("DashDistance")}, result={effectiveDistance:F2}");
        return effectiveDistance;
      }
    }
    return baseDistance;
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

    actor.velocityInfo.Speed = ApplyMovementSpeedModifier(actor.velocityInfo.walkSpeed);
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

    actor.velocityInfo.Speed = ApplyMovementSpeedModifier(actor.velocityInfo.sprintSpeed);
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

    actor.velocityInfo.Speed = ApplyMovementSpeedModifier(actor.velocityInfo.airborneSpeed);
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

    actor.velocityInfo.Speed = ApplyMovementSpeedModifier(actor.velocityInfo.airborneSpeed);
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

    actor.velocityInfo.Speed = ApplyMovementSpeedModifier(actor.velocityInfo.airborneSpeed);
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

    float effectiveDashDistance = ApplyDashDistanceModifier(actor.velocityInfo.dashDistance);
    if(actor is Player player) {
      SocketComponent socket = player.GetComponent<SocketComponent>();
      if(socket != null && socket.HasModifier("EchoStep")) {
        float echoDistance = socket.GetModifier("EchoStep") * 0.25f;
        actor.GlobalPosition += direction * echoDistance;
        GD.Print($">>> DEBUG EchoStep: value={socket.GetModifier("EchoStep")}, added={echoDistance:F2} units during dash");
      }
    }

    velocityComponent.SetDashBoost(effectiveDashDistance * 0.4f);
    actor.velocityInfo.Speed = 1.0f;
    GD.Print($">>> DEBUG Dash: distance={effectiveDashDistance:F2}, duration={actor.velocityInfo.dashDuration:F2}, cooldown={actor.velocityInfo.dashCooldown:F2}");
  }

  public override void PhysicsUpdate(double delta) {
    float effectiveDashDistance = ApplyDashDistanceModifier(actor.velocityInfo.dashDistance);
    velocityComponent
      .AccelerateInDirection(
        direction * (effectiveDashDistance / actor.velocityInfo.dashDuration)
      );
  }

  public override void Exit() {
    velocityComponent.ClearDashBoost();
    velocityComponent.Stop();
  }
}

