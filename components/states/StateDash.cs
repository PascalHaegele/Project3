using Godot;

[GlobalClass]
public partial class StateDash : State {
  public Timer cooldownTimer = new();
  private float durationTimer;

  private Vector3 direction;

  public override void CheckRelevance() {
    if(durationTimer > 0.0f) { return; }
    if(!actor.IsOnFloor()) {
      EmitSignalTransition(stateMachine.GetState<StateFall>()); return;
    }
    if(input.jump) {
      EmitSignalTransition(stateMachine.GetState<StateJump>()); return;
    }
    if(input.direction == Vector2.Zero) {
      EmitSignalTransition(stateMachine.GetState<StateIdle>()); return;
    }
    if(input.sprint) {
      EmitSignalTransition(stateMachine.GetState<StateSprint>()); return;
    }
    EmitSignalTransition(stateMachine.GetState<StateWalk>()); return;
  }

  public override void Init(Actor actor, StateMachine stateMachine) {
    base.Init(actor, stateMachine);
  }

  public override void Start() {
    cooldownTimer.OneShot = true;
    stateMachine.AddChild(cooldownTimer);

    soundLevel = 6;
  }

  public override void Enter() {
    durationTimer = velocityInfo.dashDuration;
    cooldownTimer.Start(velocityInfo.dashCooldown);

    direction = actor.Direction;
    if(direction == Vector3.Zero) { direction = -actor.Basis.Z; }

    velocityInfo.Speed = 1.0f;
  }

  public override void PhysicsUpdate(double delta) {
    durationTimer -= (float)delta;
    velocityComponent
      .AccelerateInDirection(
        direction * (velocityInfo.dashDistance / velocityInfo.dashDuration)
      );
  }

  public override void Exit() => velocityComponent.Stop();
}

