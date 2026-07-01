using Godot;

[GlobalClass]
public partial class StateWalk : State {
  [Export]
  private float speed = 6.0f;

  public override void CheckRelevance() {
    if(input.direction == Vector2.Zero) {
      EmitSignalTransition(stateMachine.GetState(typeof(StateIdle)));
      return;
    }
    if(input.sprint) {
      EmitSignalTransition(stateMachine.GetState(typeof(StateSprint)));
      return;
    }
  }

  public override void Enter() {
    actorVelocityStats.Speed = speed;
  }
}

