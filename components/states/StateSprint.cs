using Godot;

[GlobalClass]
public partial class StateSprint : State {
  [Export] private float speed = 10.0f;

  public override void CheckRelevance() {
    if(!actor.IsOnFloor()) {
        EmitSignalTransition(stateMachine.GetState<StateFall>()); return;
    }
    if(Input.jump) {
      EmitSignalTransition(stateMachine.GetState<StateJump>()); return;
    }
    if(Input.dash) {
      StateDash dashState = stateMachine.GetState<StateDash>();
      if(dashState.cooldownTimer.TimeLeft > 0.0) { return; }
      EmitSignalTransition(dashState); return;
    }
    if(Input.direction == Vector2.Zero) {
      EmitSignalTransition(stateMachine.GetState<StateIdle>()); return;
    }
    if(!Input.sprint) {
      EmitSignalTransition(stateMachine.GetState<StateWalk>()); return;
    }
  }

  public override void Enter() => actorVelocityInfo.Speed = speed;

  public override void PhysicsUpdate(double delta) {
    actorVelocityComponent.AccelerateInDirection(actor.Direction);
  }
}

