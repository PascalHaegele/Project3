using Godot;

[GlobalClass]
public partial class StateSprint : State {
  [Export]
  private float speed = 10.0f;

  public override void CheckRelevance() {
    if(input.direction == Vector2.Zero) {
      EmitSignalTransition(stateMachine.GetState(typeof(StateIdle)));
      return;
    }
    if(!input.sprint) {
      EmitSignalTransition(stateMachine.GetState(typeof(StateWalk)));
      return;
    }
  }

  public override void Enter() {
    actorVelocityStats.Speed = 10.0f;
  }
}

