using Godot;

[GlobalClass]
public partial class AIStateSearch : AIState {
  private float searchTimer;
  private float searchDuration = 3.0f;

  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(actor.playerInVision) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
    if(searchTimer >= searchDuration) {
      EmitSignalTransition(stateMachine.GetState<AIStatePatrol>()); return;
    }
    if(leashDistance > actor.leashLength) {
      EmitSignalTransition(stateMachine.GetState<AIStatePatrol>()); return;
    }
  }

  public override void Enter() => searchTimer = 0.0f;

  public override void PhysicsUpdate(double delta) {
    searchTimer += (float)delta;
  }
}

