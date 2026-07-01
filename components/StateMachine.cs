using Godot;

[GlobalClass]
public partial class StateMachine : Node {
  [Export]
  private State[] states;
  [Export]
  private State startingState;
  private State currentState;

  [Export]
  public Actor actor;
  public VelocityStats actorVelocityStats;

  public InputPackage input = new();

  public override async void _Ready() {
    _ = await ToSignal(actor, Node.SignalName.Ready);

    foreach(State state in states) {
      state.Init(actorVelocityStats, this);
      state.Transition += OnStateTransition;
    }

    currentState = startingState?? states[0];

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

  public State GetState(System.Type request) {
    foreach(State state in states) {
      if(state.GetType() == request) { return state; }
    }
    GD.PrintErr($"{actor.Name} StateMachine does not have requested State");
    return null;
  }

  private void OnStateTransition(State newState) {
    if(GetState(newState.GetType()) == null) {
      GD.PrintErr($"{actor.Name} StateMachine transition failed");
      return;
    }

    if(newState == currentState) { return; }

    currentState.Exit();
    currentState = newState;
    newState.Enter();

    GD.Print($"{actor.Name} transitioned to {currentState.GetType()}");
  }
}

