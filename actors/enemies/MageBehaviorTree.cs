using Godot;

[GlobalClass]
public partial class MageBehaviorTree : BehaviorTree {
  private RangedAttackComponent rangedAttack;

  public override void _Ready() {
    base._Ready();
    rangedAttack = GetParent<MageEnemy>().GetComponent<RangedAttackComponent>();
  }

  protected override BehaviorTreeNode ConstructTree() {
    SelectorNode root = new();

    SequenceNode combatSequence = new();
    combatSequence.AddChildren(
      new ConditionNode(
        () =>
          aiInfo.hasTarget &&
          enemy.LeashDistance < enemy.enemyInfo.leashLength
      ),
      new TaskNode(HandleCombat)
    );

    SequenceNode investigateSequence = new();
    investigateSequence.AddChildren(
      new ConditionNode(() => aiInfo.soundHeard),
      new TaskNode(LookToSound),
      new TaskNode(MoveToSound)
    );

    SequenceNode patrolSequence = new();
    patrolSequence.AddChildren(
      new ConditionNode(() => enemy.enemyInfo.HasPatrol),
      new TaskNode(MoveToNextWaypoint)
    );

    SequenceNode idleSequence = new();

    root.AddChildren(
      combatSequence,
      investigateSequence,
      patrolSequence,
      idleSequence
    );
    return root;
  }

  private NodeState HandleCombat() {
    float distance = enemy.GlobalPosition.DistanceTo(aiInfo.targetPosition);

    // If we're in preferred range, stop moving and let the state machine handle attacks
    if (rangedAttack != null && rangedAttack.IsInPreferredRange(distance)) {
      input.direction = Vector2.Zero;
      input.sprint = false;

      // Face the player
      Vector3 lookDir = aiInfo.targetPosition - enemy.GlobalPosition;
      lookDir.Y = 0.0f;
      if (!lookDir.IsEqualApprox(Vector3.Zero)) {
        enemy.LookAt(enemy.GlobalPosition + lookDir.Normalized());
      }

      return NodeState.SUCCESS;
    }

    // If too far, move toward player
    if (rangedAttack == null || rangedAttack.IsTooFar(distance)) {
      navAgent.TargetPosition = aiInfo.targetPosition;
      MoveToTarget();
      input.sprint = true;
      return NodeState.RUNNING;
    }

    // If too close, move backward (backpedal handled by state machine)
    if (rangedAttack != null && rangedAttack.IsTooClose(distance)) {
      // Move away from player
      Vector3 awayDir = (enemy.GlobalPosition - aiInfo.targetPosition).Normalized();
      awayDir.Y = 0.0f;
      navAgent.TargetPosition = enemy.GlobalPosition + awayDir * 15.0f;
      MoveToTarget();
      input.sprint = false;

      // Face the player while moving away
      Vector3 lookDir = aiInfo.targetPosition - enemy.GlobalPosition;
      lookDir.Y = 0.0f;
      if (!lookDir.IsEqualApprox(Vector3.Zero)) {
        enemy.LookAt(enemy.GlobalPosition + lookDir.Normalized());
      }

      return NodeState.RUNNING;
    }

    return NodeState.SUCCESS;
  }

  private NodeState LookToSound() {
    if (!aiInfo.soundPosition.IsEqualApprox(enemy.GlobalPosition)) {
      enemy.LookAt(aiInfo.soundPosition);
    }
    return NodeState.SUCCESS;
  }

  private NodeState MoveToSound() {
    navAgent.TargetPosition = aiInfo.soundPosition;
    if (navAgent.IsTargetReached()) {
      return NodeState.SUCCESS;
    }
    MoveToTarget();
    return NodeState.RUNNING;
  }

  private NodeState MoveToNextWaypoint() {
    if (enemy.enemyInfo.patrolPath == null) { return NodeState.FAILURE; }
    if (enemy.enemyInfo.patrolPath.Length < 1) { return NodeState.FAILURE; }
    if (navAgent.IsTargetReached()) {
      patrolIndex =
        Mathf.PosMod(++patrolIndex, enemy.enemyInfo.patrolPath.Length - 1);
      navAgent.TargetPosition = enemy.enemyInfo.patrolPath[patrolIndex];
      return NodeState.SUCCESS;
    }
    MoveToTarget();
    return NodeState.RUNNING;
  }

  private void MoveToTarget() {
    Vector3 position = enemy.GlobalTransform.Origin;
    Vector3 nextPathPosition = navAgent.GetNextPathPosition();
    Vector3 direction = position.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;
    input.direction = new(direction.X, direction.Z);

    if (!position.IsEqualApprox(enemy.GlobalPosition + direction)) {
      enemy.LookAt(enemy.GlobalPosition + direction);
    }
  }
}
