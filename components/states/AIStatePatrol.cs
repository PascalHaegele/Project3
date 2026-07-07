using Godot;

[GlobalClass]
public partial class AIStatePatrol : AIState {
  private int patrolIndex;

  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(actor.playerInVision) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
  }

  public override void PhysicsUpdate(double delta) {
    if(actor.patrolPath == null) { return; }
    if(actor.patrolPath.Length < 1) { return; }

    if(navAgent.IsTargetReached()) {
      patrolIndex = Mathf.PosMod(++patrolIndex, actor.patrolPath.Length - 1);
      navAgent.TargetPosition = actor.patrolPath[patrolIndex].GlobalPosition;
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

