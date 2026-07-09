using Godot;

[GlobalClass]
public partial class AIStatePatrol : AIState {
  private int patrolIndex;

  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(leashDistance > actor.info.leashLength) { return; }
    if(actor.playerInVision || actor.playerInHearing) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
  }

  public override void Enter() {
    navAgent.TargetPosition = actor.info.patrolPath[patrolIndex];
  }

  public override void PhysicsUpdate(double delta) {
    if(actor.info.patrolPath == null) { return; }
    if(actor.info.patrolPath.Length < 1) { return; }

    if(navAgent.IsTargetReached()) {
      patrolIndex =
        Mathf.PosMod(++patrolIndex, actor.info.patrolPath.Length - 1);
      navAgent.TargetPosition = actor.info.patrolPath[patrolIndex];
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

