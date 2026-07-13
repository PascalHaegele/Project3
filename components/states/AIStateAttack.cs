using Godot;

[GlobalClass]
public partial class AIStateAttack : AIState {
  public override void CheckRelevance(
    float playerDistance,
    float leashDistance
  ) {
    if(playerDistance > actor.enemyInfo.attackRange) {
      EmitSignalTransition(stateMachine.GetState<AIStateChase>()); return;
    }
  }

  public override void Enter() {
    actor.animationPlayer.Play("attack");
    actor.animationPlayer.AnimationFinished += OnAnimationFinished;
    actor.GetComponent<HitboxComponent>().EnableCollisionShapes();
  }

  public override void Exit() {
    actor.animationPlayer.AnimationFinished -= OnAnimationFinished;
    actor.GetComponent<HitboxComponent>().DisableCollisionShapes();
  }

  private void OnAnimationFinished(StringName animName) {
    actor.animationPlayer.Play("attack");
  }
}

