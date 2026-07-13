using Godot;

[GlobalClass]
public partial class AIStatePatrol : AIState {
  private int patrolIndex;

  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(leashDistance > actor.enemyInfo.leashLength) { return; }
    if(actor.playerInVision || actor.hearingPlayer) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
  }

  public override void Enter() {
    navAgent.TargetPosition = actor.enemyInfo.patrolPath[patrolIndex];
  }

  public override void PhysicsUpdate(double delta) {
    if(actor.enemyInfo.patrolPath == null) { return; }
    if(actor.enemyInfo.patrolPath.Length < 1) { return; }

    if(navAgent.IsTargetReached()) {
      patrolIndex =
        Mathf.PosMod(++patrolIndex, actor.enemyInfo.patrolPath.Length - 1);
      navAgent.TargetPosition = actor.enemyInfo.patrolPath[patrolIndex];
    }

    Vector3 actorPosition = actor.GlobalTransform.Origin;
    Vector3 nextPathPosition = navAgent.GetNextPathPosition();

    Vector3 direction =
      actorPosition.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;

    input.direction = new(direction.X, direction.Z);

    if(!actorPosition.IsEqualApprox(actorPosition + actor.Direction)) {
      actor.LookAt(actorPosition + actor.Direction);
    }
  }
}

