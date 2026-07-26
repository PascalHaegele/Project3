using Godot;

[GlobalClass]
public partial class MageEnemyStateMachine : TransitionStateMachine {
  private Enemy actor;
  private RangedAttackComponent rangedAttack;

  public override void _Ready() {
    actor = GetParent<Enemy>();
    rangedAttack = actor.GetComponent<RangedAttackComponent>();
    base._Ready();
  }

  protected override void SetupStates() {
    AddState("idle", new ActorStateIdle(actor, this));
    AddState("walk", new ActorStateWalk(actor, this));
    AddState("backpedal", new MageBackpedalState(actor, this));
    AddState("attack_charge", new MageAttackChargeState(actor, this, rangedAttack));
    AddState("attack_cooldown", new MageAttackCooldownState(actor, this, rangedAttack));
    AddState("fall", new ActorStateFall(actor, this));
    AddState("land", new ActorStateLand(actor, this));
  }

  protected override void SetupTransitions() {
    AddGlobalTransition(
      "fall",
      () => !actor.IsOnFloor() && actor.Velocity.Y < 0.0f
    );

    // Idle transitions
    AddTransition("idle", "walk",
      () => input.direction != Vector2.Zero && actor.aiInfo.hasTarget);
    AddTransition("idle", "attack_charge",
      () => {
        float dist = actor.GlobalPosition.DistanceTo(actor.aiInfo.targetPosition);
        return actor.aiInfo.hasTarget && rangedAttack != null && !rangedAttack.IsOnCooldown && rangedAttack.IsInPreferredRange(dist);
      });

    // Walk transitions
    AddTransition("walk", "idle",
      () => input.direction == Vector2.Zero || !actor.aiInfo.hasTarget);
    AddTransition("walk", "backpedal",
      () => {
        float dist = actor.GlobalPosition.DistanceTo(actor.aiInfo.targetPosition);
        return rangedAttack != null && rangedAttack.IsTooClose(dist);
      });

    // Backpedal transitions
    AddTransition("backpedal", "walk",
      () => {
        float dist = actor.GlobalPosition.DistanceTo(actor.aiInfo.targetPosition);
        return rangedAttack == null || !rangedAttack.IsTooClose(dist);
      });
    AddTransition("backpedal", "attack_cooldown",
      () => {
        float dist = actor.GlobalPosition.DistanceTo(actor.aiInfo.targetPosition);
        return rangedAttack != null && rangedAttack.IsInPreferredRange(dist) && !rangedAttack.IsOnCooldown && !rangedAttack.IsCharging;
      });

    // Attack charge -> cooldown is handled by RangedAttackComponent
    AddTransition("attack_charge", "attack_cooldown",
      () => rangedAttack != null && !rangedAttack.IsCharging);

    // Cooldown transitions
    AddTransition("attack_cooldown", "walk",
      () => {
        float dist = actor.GlobalPosition.DistanceTo(actor.aiInfo.targetPosition);
        return rangedAttack == null || rangedAttack.IsTooFar(dist);
      });
    AddTransition("attack_cooldown", "backpedal",
      () => {
        float dist = actor.GlobalPosition.DistanceTo(actor.aiInfo.targetPosition);
        return rangedAttack != null && rangedAttack.IsTooClose(dist);
      });
    AddTransition("attack_cooldown", "idle",
      () => !actor.aiInfo.hasTarget);
    AddTransition("attack_cooldown", "attack_charge",
      () => {
        float dist = actor.GlobalPosition.DistanceTo(actor.aiInfo.targetPosition);
        return rangedAttack != null && !rangedAttack.IsOnCooldown && rangedAttack.IsInPreferredRange(dist);
      });

    // Fall/Land
    AddTransition("fall", "land", actor.IsOnFloor);
    AddTransition("land", "idle", () => input.direction == Vector2.Zero);
    AddTransition("land", "walk", () => input.direction != Vector2.Zero);
  }
}

// Backpedal state - move backward away from player
public partial class MageBackpedalState : ActorState {
  private Enemy enemy;

  public MageBackpedalState(Actor actor, StateMachine stateMachine) :
    base(actor, stateMachine) {
    enemy = actor as Enemy;
  }

  public override void Start() => soundLevel = 3;

  public override void Enter() {
    base.Enter();
    actor.velocityInfo.Speed = ApplyMovementSpeedModifier(actor.velocityInfo.walkSpeed * 0.7f);
  }

  public override void PhysicsUpdate(double delta) {
    if (enemy == null) { return; }
    // Move backward (away from player)
    Vector3 awayDir = (actor.GlobalPosition - enemy.aiInfo.targetPosition).Normalized();
    awayDir.Y = 0.0f;
    if (awayDir != Vector3.Zero) {
      // Face the player while moving backward
      Vector3 lookDir = enemy.aiInfo.targetPosition - actor.GlobalPosition;
      lookDir.Y = 0.0f;
      if (lookDir != Vector3.Zero) {
        actor.LookAt(actor.GlobalPosition + lookDir.Normalized());
      }
    }
    velocityComponent.AccelerateInDirection(awayDir);
  }
}

// Attack charge state
public partial class MageAttackChargeState : ActorState {
  private RangedAttackComponent rangedAttack;
  private Enemy enemy;

  public MageAttackChargeState(Actor actor, StateMachine stateMachine, RangedAttackComponent rangedAttack) :
    base(actor, stateMachine) {
    this.rangedAttack = rangedAttack;
    enemy = actor as Enemy;
  }

  public override void Start() => soundLevel = 0;

  public override void Enter() {
    base.Enter();
    actor.velocityInfo.Speed = 0.0f;

    if (enemy != null) {
      // Face the player
      Vector3 lookDir = enemy.aiInfo.targetPosition - actor.GlobalPosition;
      lookDir.Y = 0.0f;
      if (lookDir != Vector3.Zero) {
        actor.LookAt(actor.GlobalPosition + lookDir.Normalized());
      }

      // Start the charge
      rangedAttack?.StartCharge();
    }
  }

  public override void PhysicsUpdate(double delta) {
    // Stand still while charging
    velocityComponent.Decelerate();

    if (enemy != null) {
      // Keep facing player
      Vector3 lookDir = enemy.aiInfo.targetPosition - actor.GlobalPosition;
      lookDir.Y = 0.0f;
      if (lookDir != Vector3.Zero) {
        actor.LookAt(actor.GlobalPosition + lookDir.Normalized());
      }
    }
  }

  public override void Exit() {
    base.Exit();
    rangedAttack?.CancelCharge();
  }
}

// Attack cooldown state
public partial class MageAttackCooldownState : ActorState {
  private RangedAttackComponent rangedAttack;
  private Enemy enemy;

  public MageAttackCooldownState(Actor actor, StateMachine stateMachine, RangedAttackComponent rangedAttack) :
    base(actor, stateMachine) {
    this.rangedAttack = rangedAttack;
    enemy = actor as Enemy;
  }

  public override void Start() => soundLevel = 0;

  public override void Enter() {
    base.Enter();
    actor.velocityInfo.Speed = ApplyMovementSpeedModifier(actor.velocityInfo.walkSpeed * 0.3f);
  }

  public override void PhysicsUpdate(double delta) {
    // Slowly strafe or reposition during cooldown
    velocityComponent.Decelerate();

    if (enemy != null) {
      // Face the player
      Vector3 lookDir = enemy.aiInfo.targetPosition - actor.GlobalPosition;
      lookDir.Y = 0.0f;
      if (lookDir != Vector3.Zero) {
        actor.LookAt(actor.GlobalPosition + lookDir.Normalized());
      }
    }
  }
}
