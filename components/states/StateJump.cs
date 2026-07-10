using Godot;

[GlobalClass]
public partial class StateJump : State {
  public override void CheckRelevance() {
    if(input.dash) {
      StateDash dashState = stateMachine.GetState<StateDash>();
      if(dashState.cooldownTimer.TimeLeft > 0.0) { return; }
      EmitSignalTransition(dashState); return;
    }
    if(!actor.IsOnFloor()) {
      if(actor.Velocity.Y < 0.0f) {
        EmitSignalTransition(stateMachine.GetState<StateFall>()); return;
      }
      return;
    }
    if(input.direction == Vector2.Zero) {
      EmitSignalTransition(stateMachine.GetState<StateIdle>()); return;
    }
    if(input.sprint) {
      EmitSignalTransition(stateMachine.GetState<StateSprint>()); return;
    }
    EmitSignalTransition(stateMachine.GetState<StateWalk>()); return;
  }

  public override void Start() => soundLevel = 6;

  public override void Enter() {
    velocityInfo.Speed = velocityInfo.airborneSpeed;
    velocityComponent
      .AddVelocityInDirection(actor.UpDirection, velocityInfo.jumpVelocity);
  }

  public override void PhysicsUpdate(double delta) {
    velocityComponent.AccelerateInDirection(actor.Direction);
  }
}

