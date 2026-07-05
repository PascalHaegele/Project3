using Godot;

[GlobalClass]
public partial class StateIdle : State {
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
    if(Input.direction == Vector2.Zero) { return; }
    if(Godot.Input.IsActionPressed("sprint")) {
      EmitSignalTransition(stateMachine.GetState<StateSprint>()); return;
    }
    EmitSignalTransition(stateMachine.GetState<StateWalk>()); return;
  }

  public override void Enter() => actorVelocityInfo.Speed = 0.0f;

  public override void PhysicsUpdate(double delta) {
    actorVelocityComponent.Decelerate();
  }
}

