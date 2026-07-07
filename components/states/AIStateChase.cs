using Godot;

[GlobalClass]
public partial class AIStateChase : AIState {
  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(playerDistance <= actor.attackRange) {
      EmitSignalTransition(stateMachine.GetState<AIStateAttack>()); return;
    }
    if(leashDistance > actor.leashLength) {
      EmitSignalTransition(stateMachine.GetState<AIStatePatrol>()); return;
    }
    if(!actor.playerInVision) {
      EmitSignalTransition(stateMachine.GetState<AIStateSearch>()); return;
    }
  }

  public override void PhysicsUpdate(double delta) {
    if(!navAgent.IsNavigationFinished()) {
      navAgent.TargetPosition = player.GlobalPosition;
    }

    Vector3 actorPosition = actor.GlobalTransform.Origin;
    Vector3 nextPathPosition = navAgent.GetNextPathPosition();

    Vector3 direction =
      actorPosition.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;

    input.direction = new(direction.X, direction.Z);
    input.sprint = true;

    if(!actorPosition.IsEqualApprox(actorPosition + actor.Direction)) {
      actor.LookAt(actorPosition + actor.Direction);
    }
  }
}

