using Godot;

[GlobalClass]
public partial class StateIdle : State {
  public override void CheckRelevance() {
    if(input.direction != Vector2.Zero) {
      if(Input.IsActionPressed("sprint")) {
        EmitSignalTransition(stateMachine.GetState(typeof(StateSprint)));
        return;
      }
      EmitSignalTransition(stateMachine.GetState(typeof(StateWalk)));
      return;
    }
  }
}

