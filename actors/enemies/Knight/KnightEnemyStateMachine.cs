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
    AddState("attack", new ActorStateAttack(enemy,this));
  }

  protected override void SetupTransitions() {
    // --- Globale Transitions ---
    AddGlobalTransition(
      "fall",
      () => !enemy.IsOnFloor() && enemy.Velocity.Y < 0.0f
    );

    // --- Von IDLE ---
    AddTransition("idle", "walk", () => input.direction != Vector2.Zero);
    AddTransition(
      "idle",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );
    AddTransition("idle", "attack", () => input.attack);

    // --- Von WALK ---
    AddTransition("walk", "idle", () => input.direction == Vector2.Zero);
    AddTransition(
      "walk",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );
    AddTransition("walk", "attack", () => input.attack);

    // --- Von SPRINT ---
    AddTransition("sprint", "idle", () => input.direction == Vector2.Zero);
    AddTransition("sprint", "walk", () => !input.sprint);
    AddTransition("sprint", "attack", () => input.attack);

    AddTransition("fall", "land", enemy.IsOnFloor);

    // --- Von LAND ---
    AddTransition("land", "idle", () => input.direction == Vector2.Zero);
    AddTransition("land", "walk", () => input.direction != Vector2.Zero);
    AddTransition(
      "land",
      "sprint",
      () => input.direction != Vector2.Zero && input.sprint
    );

    // --- Von ATTACK (Zurück in die Bewegung, wenn der Angriff vorbei ist) ---
    AddTransition("attack", "idle", () => !input.attack && input.direction == Vector2.Zero);
    AddTransition("attack", "walk", () => !input.attack && input.direction != Vector2.Zero);
  }
}

