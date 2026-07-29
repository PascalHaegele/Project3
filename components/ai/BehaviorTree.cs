using Godot;

[GlobalClass]
public partial class BehaviorTree : Node {
  protected AIInfo aiInfo;
  protected Enemy enemy;

  [Export] protected NavigationAgent3D navAgent;
  [Export] private float attackRange = 2.0f;

  private BehaviorTreeNode rootNode;

  [Export] private float updateInterval = 0.1f;
  private float timeSinceLastUpdate;

  protected int patrolIndex;

  protected InputPackage input = new();
  public InputPackage GetInput => input;

  public override void _Ready() {
    enemy = GetParent<Enemy>();

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

  protected virtual BehaviorTreeNode ConstructTree() {
    SelectorNode root = new();

    SequenceNode combatSequence = new();
    combatSequence.AddChildren(
      new ConditionNode(() => aiInfo.hasTarget && enemy.InsideLeashLength),
      new TaskNode(MoveToPlayer),
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
      new TaskNode(PlayIdleAnimation)
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

  private NodeState MoveToPlayer() {
    navAgent.TargetPosition = aiInfo.soundPosition;

    float distanceToTarget = enemy.GlobalPosition.DistanceTo(aiInfo.soundPosition);

    if (distanceToTarget <= attackRange) {
        return NodeState.SUCCESS;
    }

    if (enemy.animationPlayer != null && enemy.animationPlayer.CurrentAnimation != "Knight_walk") {
        enemy.animationPlayer.Play("Knight_walk");
    }

    MoveToTarget();
    return NodeState.RUNNING;
  }

  private NodeState AttackPlayer() {
    if (enemy.animationPlayer != null) {
        if (enemy.animationPlayer.CurrentAnimation == "Knight_Attack") {
          if (enemy.animationPlayer.IsPlaying()) {
            return NodeState.RUNNING;
          } else {
            enemy.animationPlayer.Stop();
            return NodeState.SUCCESS;
          }
        }

        // enemy.GetComponent<HitboxComponent>().EnableCollisionShapes();
        enemy.animationPlayer.Play("Knight_Attack", -1.0f, 2.0f);
    }

    return NodeState.RUNNING;
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

    if (enemy.animationPlayer != null && enemy.animationPlayer.CurrentAnimation != "Knight_walk") {
        enemy.animationPlayer.Play("Knight_walk");
    }

    MoveToTarget();
    return NodeState.RUNNING;
  }

  private NodeState MoveToNextWaypoint() {
    if(enemy.enemyInfo.patrolPath == null) { return NodeState.FAILURE; }
    if(enemy.enemyInfo.patrolPath.Length < 1) { return NodeState.FAILURE; }

    if(navAgent.IsTargetReached()) {
      patrolIndex = Mathf.PosMod(++patrolIndex, enemy.enemyInfo.patrolPath.Length - 1);
      navAgent.TargetPosition = enemy.enemyInfo.patrolPath[patrolIndex];

      return NodeState.SUCCESS;
    }

    if (enemy.animationPlayer != null && enemy.animationPlayer.CurrentAnimation != "Knight_walk") {
        enemy.animationPlayer.Play("Knight_walk");
    }

    MoveToTarget();
    return NodeState.RUNNING;
  }

  private NodeState PlayIdleAnimation() {
    if (enemy.animationPlayer != null && enemy.animationPlayer.CurrentAnimation != "Knight_idle") {
        enemy.animationPlayer.Play("Knight_idle");
    }
    return NodeState.SUCCESS;
  }

  private void MoveToTarget() {
    Vector3 position = enemy.GlobalTransform.Origin;
    Vector3 nextPathPosition = navAgent.GetNextPathPosition();

    Vector3 direction = position.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;

    input.direction = new(direction.X, direction.Z);

    if(!position.IsEqualApprox(enemy.GlobalPosition + direction)) {
      enemy.LookAt(enemy.GlobalPosition + direction);
    }
  }
}
