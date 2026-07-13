using Godot;
using System.Diagnostics;
using System.Linq;

[GlobalClass]
public partial class StateMachine : Node {
  [Export] private State[] states;
  [Export] private State? startingState;
  private State currentState;

  private Actor actor;
  public InputPackage input = new();

  public override void _Ready() {
    actor = GetParent<Actor>();

    foreach(State state in states) {
      state.ResourceLocalToScene = true;
      state.Init(actor, this);
      state.Transition += OnStateTransition;
      state.Start();
    }

    currentState = startingState ?? states[0];

    if(currentState == null) {
      GD.PrintErr($"{actor.Name} StateMachine has no starting State");
    }

    currentState.Enter();
  }

  public override void _Process(double delta) {
    currentState.Update(delta);
    currentState.CheckRelevance();
    currentState.input = input;
  }

  public override void _PhysicsProcess(double delta) {
    currentState.PhysicsUpdate(delta);
  }

  public T? GetState<T>() where T : State {
    foreach(State state in states) { if(state is T) { return state as T; } }

    StackFrame frame = new StackTrace().GetFrame(1);
    GD.PrintErr(
      $"{actor.Name} StateMachine does not have requested State " +
      $"{typeof(T)} | " +
      $"Requested from {frame.GetMethod().DeclaringType.Name} " +
      $"in method {frame.GetMethod().Name}"
    );
    return null;
  }

  private void OnStateTransition(State newState) {
    if(!states.Contains(newState)) {
      GD.PrintErr(
        $"{actor.Name} StateMachine does not contain {newState.GetType()}"
      );
      return;
    }

    if(newState == currentState) { return; }

    currentState.Exit();
    currentState = newState;
    currentState.Enter();

    actor.soundLevel = currentState.soundLevel;
  }
}

