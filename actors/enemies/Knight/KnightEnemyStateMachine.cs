using Godot;

[GlobalClass]
public partial class KnightEnemyStateMachine : TransitionStateMachine {
  private Enemy enemy;
  public override void _Ready() {
    enemy = GetParent<Enemy>();
    base._Ready();
  }

  protected override void SetupStates() {
    AddState("idle", new ActorStateIdle(enemy, this));
    AddState("walk", new ActorStateWalk(enemy, this));
    AddState("sprint", new ActorStateSprint(enemy, this));
    AddState("fall", new ActorStateFall(enemy, this));
    AddState("land", new ActorStateLand(enemy, this));
  }

  protected override void SetupTransitions() {
    AddGlobalTransition(
      "fall",
      () => !enemy.IsOnFloor() && enemy.Velocity.Y < 0.0f
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

    AddTransition("fall", "land", enemy.IsOnFloor);

    AddTransition("land", "idle", () => input.direction == Vector2.Zero);
    AddTransition("land", "walk", () => input.direction != Vector2.Zero);
    AddTransition(
      "land",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );
  }
}

