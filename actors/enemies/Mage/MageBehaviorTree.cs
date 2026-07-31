using Godot;

[GlobalClass]
public partial class MageBehaviorTree : Node {
  private AIInfo aiInfo;

  protected MageEnemy enemy;

  [Export] protected NavigationAgent3D navAgent;

  private BehaviorTreeNode rootNode;

  [Export] private float updateInterval = 0.1f;
  private float timeSinceLastUpdate;

  protected int patrolIndex;

  protected InputPackage input = new();
  public InputPackage GetInput => input;

  public override void _Ready() {
    enemy = GetParent<MageEnemy>();

    navAgent ??= GetNode<NavigationAgent3D>("../NavigationAgent3D");
    if(enemy.enemyInfo.patrolPath?.Length > 0) {
      navAgent.TargetPosition = enemy.enemyInfo.patrolPath[patrolIndex];
    }

    rootNode = ConstructTree();
  }

  public override void _Process(double delta) {
    timeSinceLastUpdate += (float)delta;
    if(timeSinceLastUpdate < updateInterval) { return; }

    timeSinceLastUpdate = 0.0f;
    input = new();

    _ = rootNode.Evaluate();
  }

  public void UpdateInfo(AIInfo info) => aiInfo = info;

  private BehaviorTreeNode ConstructTree() {
    SelectorNode root = new();

    SequenceNode combatSequence = new();
    combatSequence.AddChildren(
      new ConditionNode(
        () =>
          aiInfo.hasTarget &&
          enemy.LeashDistance < enemy.enemyInfo.leashLength
      ),
      new TaskNode(MoveIntoRange),
      new ConditionNode(() => enemy.CanShoot),
      new TaskNode(AttackPlayer)
    );

    SequenceNode shotAtSequence = new();
    shotAtSequence.AddChildren(
      new ConditionNode(() => aiInfo.beeingShot && !aiInfo.playerVisible),
      new TaskNode(LookToShot)
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
    idleSequence.AddChildren(
      new TaskNode(Idle)
    );

    root.AddChildren(
      combatSequence,
      shotAtSequence,
      investigateSequence,
      patrolSequence,
      idleSequence
    );
    return root;
  }

  private NodeState MoveIntoRange() {
    if(enemy.TargetInRange) { return NodeState.SUCCESS; }

    float distance = enemy.DistanceToTarget;
    if(distance < enemy.enemyInfo.minAttackRange) {
      Vector3 dir = (enemy.GlobalPosition - aiInfo.targetPosition).Normalized();
      dir.Y = enemy.GlobalPosition.Y;
      navAgent.TargetPosition = enemy.GlobalPosition + dir;
      MoveToTarget(false);

      if(!enemy.GlobalPosition.IsEqualApprox(aiInfo.targetPosition)) {
        enemy.LookAt(aiInfo.targetPosition);
      }
    } else {
      Vector3 dir =
        enemy.GlobalPosition.DirectionTo(aiInfo.targetPosition).Normalized();
      dir.Y = enemy.GlobalPosition.Y;
      navAgent.TargetPosition = enemy.GlobalPosition + dir;
      MoveToTarget();
    }

    return NodeState.RUNNING;
  }

  private NodeState AttackPlayer() {
    // if(enemy.animationPlayer.IsPlaying()) { return NodeState.RUNNING; }
    enemy.animationPlayer.Play("shoot");
    // input.shoot = true;
    return NodeState.SUCCESS;
  }

  private NodeState LookToShot() {
    if(aiInfo.shotFromDirection != Vector3.Zero) {
      enemy.LookAt(enemy.GlobalPosition + aiInfo.shotFromDirection);
    }

    return NodeState.SUCCESS;
  }

  private NodeState LookToSound() {
    if(!aiInfo.soundPosition.IsEqualApprox(enemy.GlobalPosition)) {
      enemy.LookAt(aiInfo.soundPosition);
    }

    return NodeState.SUCCESS;
  }

  private NodeState MoveToSound() {
    navAgent.TargetPosition = aiInfo.soundPosition;

    if(navAgent.IsTargetReached()) {
      return NodeState.SUCCESS;
    }

    MoveToTarget();

    return NodeState.RUNNING;
  }

  private NodeState MoveToNextWaypoint() {
    if(navAgent.IsTargetReached()) {
      patrolIndex =
        Mathf.PosMod(++patrolIndex, enemy.enemyInfo.patrolPath.Length);
      navAgent.TargetPosition = enemy.enemyInfo.patrolPath[patrolIndex];
      return NodeState.SUCCESS;
    }

    MoveToTarget();

    return NodeState.RUNNING;
  }

  private NodeState Idle() {
    if(!enemy.animationPlayer.IsPlaying()) {
      enemy.animationPlayer.Play("idle");
      return NodeState.RUNNING;
    }
    return NodeState.SUCCESS;
  }

  private void MoveToTarget(bool lookAtTarget = true) {
    Vector3 position = enemy.GlobalTransform.Origin;
    Vector3 nextPathPosition = navAgent.GetNextPathPosition();

    Vector3 direction = position.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;

    input.direction = new(direction.X, direction.Z);

    if(lookAtTarget) {
      Vector3 lookAtPosition = position + direction;

      if(position.DistanceSquaredTo(lookAtPosition) > 0.1) {
        enemy.LookAtFromPosition(position, lookAtPosition);
      }
    }
  }
}
