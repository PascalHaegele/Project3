using Godot;

[GlobalClass]
public partial class StateIdle : State {
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
    if(input.direction == Vector2.Zero) { return; }
    if(input.sprint) {
      EmitSignalTransition(stateMachine.GetState<StateSprint>()); return;
    }
    EmitSignalTransition(stateMachine.GetState<StateWalk>()); return;
  }

  public override void Start() => soundLevel = 0;

  public override void Enter() => velocityInfo.Speed = 0.0f;

  public override void PhysicsUpdate(double delta) {
    velocityComponent.Decelerate();
  }
}

