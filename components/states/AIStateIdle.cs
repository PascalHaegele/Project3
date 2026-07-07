using Godot;

[GlobalClass]
public partial class AIStateIdle : AIState {
  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(actor.playerInVision) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
  }
}

