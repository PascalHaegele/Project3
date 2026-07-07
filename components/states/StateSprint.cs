using Godot;

[GlobalClass]
public partial class StateSprint : State {
  public override void CheckRelevance() {
    if(!actor.IsOnFloor()) {
        EmitSignalTransition(stateMachine.GetState<StateFall>()); return;
    }
    if(input.jump) {
      EmitSignalTransition(stateMachine.GetState<StateJump>()); return;
    }
    if(input.dash) {
      StateDash dashState = stateMachine.GetState<StateDash>();
      if(dashState.cooldownTimer.TimeLeft > 0.0) { return; }
      EmitSignalTransition(dashState); return;
    }
    if(input.direction == Vector2.Zero) {
      EmitSignalTransition(stateMachine.GetState<StateIdle>()); return;
    }
    if(!input.sprint) {
      EmitSignalTransition(stateMachine.GetState<StateWalk>()); return;
    }
  }

  public override void Enter() {
    velocityInfo.Speed = velocityInfo.sprintSpeed;
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.AccelerateInDirection(actor.Direction);
  }
}

