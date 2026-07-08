using Godot;

[GlobalClass]
public partial class AIStateAttack : AIState {
  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(playerDistance > actor.info.attackRange) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
  }

  public override void Enter() {
    actor.animationPlayer.Play("attack");
  }
}

