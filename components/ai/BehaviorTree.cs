using Godot;

[GlobalClass]
public partial class BehaviorTree : Node {
  private AIInfo aiInfo;

  private Enemy enemy;

  [Export] private NavigationAgent3D navAgent;

  private BehaviorTreeNode rootNode;
  private BehaviorTreeNode runningNode;

  [Export] private float updateInterval = 0.1f;
  private float timeSinceLastUpdate;

  private int patrolIndex;

  private float waypointTimer;

  private InputPackage input = new();
  public InputPackage GetInput => input;

  public override void _Ready() {
    enemy = GetParent<Enemy>();

    navAgent ??= GetNode<NavigationAgent3D>("../NavigationAgent3D");
    navAgent.TargetPosition = enemy.enemyInfo.patrolPath[patrolIndex];

    rootNode = ConstructTree();
  }

  public override void _Process(double delta) {
    timeSinceLastUpdate += (float)delta;
    if(timeSinceLastUpdate < updateInterval) { return; }

    timeSinceLastUpdate = 0.0f;
    input = new();

    NodeState result = rootNode.Evaluate();

    // if(result == NodeState.SUCCESS) {
    //   GD.Print("BehaviorTree completed successfully");
    // } else if(result == NodeState.FAILURE) {
    //   GD.Print("BehaviorTree failed");
    // }
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
      new TaskNode(MoveToPlayer),
      new TaskNode(AttackPlayer)
    );

    SequenceNode investigateSequence = new();
    investigateSequence.AddChildren(
      new ConditionNode(() => aiInfo.soundHeard),
      new TaskNode(LookToSound)
    );

    SequenceNode patrolSequence = new();
    patrolSequence.AddChildren(new TaskNode(MoveToNextWaypoint));

    root.AddChildren(combatSequence, investigateSequence, patrolSequence);
    // root.AddChildren(combatSequence, patrolSequence);
    return root;
  }

  private NodeState MoveToPlayer() {
    float distance = enemy.GlobalPosition.DistanceTo(aiInfo.targetPosition);
    if(distance <= enemy.enemyInfo.attackRange) { return NodeState.SUCCESS; }

    navAgent.TargetPosition = aiInfo.targetPosition;

    Vector3 position = enemy.GlobalTransform.Origin;
    Vector3 nextPathPosition = navAgent.GetNextPathPosition();

    Vector3 direction =
      position.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;

    input.direction = new(direction.X, direction.Z);
    input.sprint = true;

    if(!position.IsEqualApprox(enemy.GlobalPosition + direction)) {
      enemy.LookAt(enemy.GlobalPosition + direction);
    }

    return NodeState.RUNNING;
  }

  private NodeState AttackPlayer() {
    enemy.animationPlayer.Play("attack");
    return NodeState.SUCCESS;
  }

  private NodeState LookToSound() {
    if(!aiInfo.soundPosition.IsEqualApprox(enemy.GlobalPosition)) {
      enemy.LookAt(aiInfo.soundPosition);
    }

    return NodeState.SUCCESS;
  }

  private NodeState MoveToNextWaypoint() {
    if(enemy.enemyInfo.patrolPath == null) { return NodeState.FAILURE; }
    if(enemy.enemyInfo.patrolPath.Length < 1) { return NodeState.FAILURE; }

    if(navAgent.IsTargetReached()) {
      patrolIndex =
        Mathf.PosMod(++patrolIndex, enemy.enemyInfo.patrolPath.Length - 1);
      navAgent.TargetPosition = enemy.enemyInfo.patrolPath[patrolIndex];

      return NodeState.SUCCESS;
    }

    Vector3 actorPosition = enemy.GlobalTransform.Origin;
    Vector3 nextPathPosition = navAgent.GetNextPathPosition();

    Vector3 direction =
      actorPosition.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;

    input.direction = new(direction.X, direction.Z);

    if(!actorPosition.IsEqualApprox(actorPosition + enemy.Direction)) {
      enemy.LookAt(actorPosition + enemy.Direction);
    }

    return NodeState.RUNNING;
  }
}

