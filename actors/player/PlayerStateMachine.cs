using Godot;

[GlobalClass]
public partial class PlayerStateMachine : TransitionStateMachine {
  public float dashCooldownTimer;

  private Player actor;

  private bool DashComplete =>
    currentState.TimeInState >= actor.velocityInfo.dashDuration;

  public override void _Ready() {
    actor = GetParent<Player>();
    base._Ready();
  }

  public override void _Process(double delta) {
    base._Process(delta);
    Debug.panel.AddProperty("Current State", currentState.name, 1);
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);

    if(currentState is ActorStateDash dashState) {
      if(dashState.TimeInState < actor.velocityInfo.dashDuration) { return; }
    }
  }

  protected override void SetupStates() {
    AddState("idle", new ActorStateIdle(actor, this));
    AddState("walk", new ActorStateWalk(actor, this));
    AddState("sprint", new ActorStateSprint(actor, this));
    AddState("jump", new ActorStateJump(actor, this));
    AddState("fall", new ActorStateFall(actor, this));
    AddState("land", new ActorStateLand(actor, this));
    AddState("dash", new ActorStateDash(actor, this));
  }

  protected override void SetupTransitions() {
    AddGlobalTransition(
      "dash",
      () => input.dash && GetState<ActorStateDash>().cooldownTimer.IsStopped()
    );

    AddGlobalTransition(
      "fall",
      () => !actor.IsOnFloor() && actor.Velocity.Y < 0.0f
    );

    AddTransition("idle", "jump", () => input.jump);
    AddTransition("idle", "walk", () => input.direction != Vector2.Zero);
    AddTransition(
      "idle",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );

    AddTransition("walk", "jump", () => input.jump);
    AddTransition("walk", "idle", () => input.direction == Vector2.Zero);
    AddTransition(
      "walk",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );

    AddTransition("sprint", "jump", () => input.jump);
    AddTransition("sprint", "idle", () => input.direction == Vector2.Zero);
    AddTransition("sprint", "walk", () => !input.sprint);

    AddTransition("jump", "land", actor.IsOnFloor);

    AddTransition("fall", "land", actor.IsOnFloor);

    AddTransition("land", "idle", () => input.direction == Vector2.Zero);
    AddTransition("land", "walk", () => input.direction != Vector2.Zero);
    AddTransition(
      "land",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );

    AddTransition("dash", "jump", () => DashComplete && input.jump);
    AddTransition(
      "dash",
      "idle",
      () => DashComplete && input.direction == Vector2.Zero
    );
    AddTransition(
      "dash",
      "walk",
      () => DashComplete && input.direction != Vector2.Zero
    );
    AddTransition(
      "dash",
      "sprint",
      () => DashComplete && input.direction != Vector2.Zero && input.sprint
    );

  }

  protected override void ChangeState(StateV2 newState) {
    base.ChangeState(newState);
    if(currentState is ActorState actorState) {
      actor.soundLevel = actorState.soundLevel;
    }
  }

  protected override void ChangeState(StringName newStateName) {
    base.ChangeState(newStateName);
    if(currentState is ActorState actorState) {
      actor.soundLevel = actorState.soundLevel;
    }
  }
}

