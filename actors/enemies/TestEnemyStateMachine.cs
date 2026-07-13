using Godot;

[GlobalClass]
public partial class TestEnemyStateMachine : TransitionStateMachine {
  private Enemy actor;
  public override void _Ready() {
    actor = GetParent<Enemy>();
    base._Ready();
  }

  protected override void SetupStates() {
    AddState("idle", new ActorStateIdle(actor, this));
    AddState("walk", new ActorStateWalk(actor, this));
    AddState("sprint", new ActorStateSprint(actor, this));
    AddState("fall", new ActorStateFall(actor, this));
    AddState("land", new ActorStateLand(actor, this));
  }

  protected override void SetupTransitions() {
    AddGlobalTransition(
      "fall",
      () => !actor.IsOnFloor() && actor.Velocity.Y < 0.0f
    );

    AddTransition("idle", "walk", () => input.direction != Vector2.Zero);
    AddTransition(
      "idle",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );

    AddTransition("walk", "idle", () => input.direction == Vector2.Zero);
    AddTransition(
      "walk",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );

    AddTransition("sprint", "idle", () => input.direction == Vector2.Zero);
    AddTransition("sprint", "walk", () => !input.sprint);

    AddTransition("fall", "land", actor.IsOnFloor);

    AddTransition("land", "idle", () => input.direction == Vector2.Zero);
    AddTransition("land", "walk", () => input.direction != Vector2.Zero);
    AddTransition(
      "land",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );
  }
}

