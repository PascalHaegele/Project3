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

    NodeState result = rootNode.Evaluate();

    if(result == NodeState.SUCCESS) {
      GD.Print("BehaviorTree completed successfully");
    } else if(result == NodeState.FAILURE) {
      GD.Print("BehaviorTree failed");
    }
  }

  public void UpdateInfo(AIInfo info) => aiInfo = info;

  private BehaviorTreeNode ConstructTree() {
    SelectorNode root = new();

    SequenceNode combatSequence = new();
    combatSequence.AddChildren(
      new ConditionNode(() => aiInfo.playerVisible),
      new TaskNode(MoveToPlayer),
      new TaskNode(AttackPlayer)
    );

    SequenceNode investigateSequence = new();
    investigateSequence.AddChildren(
      new ConditionNode(() => aiInfo.soundHeard),
      new TaskNode(MoveToSound),
      new TaskNode(LookAround)
    );

    SequenceNode patrolSequence = new();
    patrolSequence.AddChildren(
      new TaskNode(MoveToNextWaypoint),
      new TaskNode(WaitAtWaypoint)
    );

    root.AddChildren(combatSequence, investigateSequence, patrolSequence);
    return root;
  }

  private NodeState MoveToPlayer() {
    float distance =
      enemy.GlobalPosition.DistanceTo(aiInfo.targetPosition);
    if(distance > enemy.enemyInfo.attackRange) {

    }

    return NodeState.SUCCESS;
  }

  private NodeState AttackPlayer() {
    return NodeState.SUCCESS;
  }

  private NodeState MoveToSound() {
    return NodeState.SUCCESS;
  }

  private NodeState LookAround() {
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
    return NodeState.RUNNING;
  }

  private NodeState WaitAtWaypoint() {

    return NodeState.SUCCESS;
  }
}

