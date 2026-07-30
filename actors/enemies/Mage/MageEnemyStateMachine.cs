using Godot;

[GlobalClass]
public partial class MageEnemyStateMachine : TransitionStateMachine {
  private MageEnemy enemy;

  public override void _Ready() {
    enemy = GetParent<MageEnemy>();
    base._Ready();
  }

  protected override void SetupStates() {
    AddState("idle", new ActorStateIdle(enemy, this));
    AddState("walk", new ActorStateWalk(enemy, this));
    AddState("fall", new ActorStateFall(enemy, this));
    AddState("land", new ActorStateLand(enemy, this));
  }

  protected override void SetupTransitions() {
    AddGlobalTransition(
      "fall",
      () => !enemy.IsOnFloor() && enemy.Velocity.Y < 0.0f
    );

    // Idle transitions
    AddTransition(
      "idle",
      "walk",
      () => input.direction != Vector2.Zero && enemy.aiInfo.hasTarget
    );

    // Walk transitions
    AddTransition(
      "walk",
      "idle",
      () => input.direction == Vector2.Zero || !enemy.aiInfo.hasTarget
    );

    // Fall/Land
    AddTransition("fall", "land", enemy.IsOnFloor);

    AddTransition("land", "idle", () => input.direction == Vector2.Zero);
    AddTransition("land", "walk", () => input.direction != Vector2.Zero);
  }
}

