using Godot;

[GlobalClass]
public partial class AIStateAttack : AIState {
  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(playerDistance > actor.attackRange) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
  }
}

