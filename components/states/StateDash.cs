using Godot;

[GlobalClass]
public partial class StateDash : State {
  [Export] public float distance = 8f;
  [Export] public float duration = 0.2f;
  [Export] public float cooldown = 0.8f;

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
    cooldownTimer.OneShot = true;
    base.stateMachine.AddChild(cooldownTimer);
  }

  public override void Enter() {
    durationTimer = duration;
    cooldownTimer.Start(cooldown);

    direction = actor.Direction;
    if(direction == Vector3.Zero) { direction = -actor.Basis.Z; }

    velocityInfo.Speed = 1.0f;
  }

  public override void PhysicsUpdate(double delta) {
    durationTimer -= (float)delta;
    velocityComponent
      .AccelerateInDirection(direction * (distance / duration));
  }

  public override void Exit() => velocityComponent.Stop();
}

