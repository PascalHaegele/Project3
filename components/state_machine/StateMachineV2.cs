using Godot;
using System.Collections.Generic;
using System.Diagnostics;

[GlobalClass]
public abstract partial class StateMachineV2 : Node {
  protected List<StateV2> states = new();
  protected StateV2 currentState;
  protected StateV2 previousState;
  protected StateV2? startingState;

  protected const int maxHistory = 50;
  protected List<StringName> stateHistory = new();

  protected bool debug;
  protected int stateChangeCount;

  protected InputPackage input = new();

  public StringName[] GetStateHistory => [.. stateHistory];

  public Dictionary<string, object> GetDebugInfo => new() {
    { "current state", currentState.GetType() },
    { "previous state", previousState?.GetType() },
    { "state count", states.Count },
    { "change count", stateChangeCount },
    { "history", stateHistory[^5..] },
  };

  [Signal]
  public delegate void StateChangedEventHandler(StateV2 from, StateV2 to);
  [Signal] public delegate void StateEnteredEventHandler(StateV2 state);
  [Signal] public delegate void StateExitedEventHandler(StateV2 state);

  public override void _Ready() {
    SetupStates();
    if(startingState != null) { ChangeState(startingState); }
    else if(states.Count > 0) { ChangeState(states[0]); }
    else { GD.PrintErr($"{Name} has no States"); }
  }

  public override void _Process(double delta) {
    currentState?.Update(delta);
  }

  public override void _PhysicsProcess(double delta) {
    currentState?.PhysicsUpdate(delta);
  }

  public T? GetState<T>() where T : StateV2 {
    foreach(StateV2 state in states) { if(state is T) { return state as T; } }

    StackFrame frame = new StackTrace().GetFrame(1);
    GD.PrintErr(
      $"{Name} does not have requested State {typeof(T)} | " +
      $"Requested from {frame.GetMethod().DeclaringType.Name} " +
      $"in method {frame.GetMethod().Name}"
    );
    return null;
  }

  public T? GetStateOrNull<T>() where T : StateV2 {
    return states.Find((state) => state is T) as T;
  }

  public T? GetState<T>(StringName stateName) where T : StateV2 {
    StateV2? state = states.Find((s) => s.name == stateName);
    if(state is not null and T) { return state as T; }

    StackFrame frame = new StackTrace().GetFrame(1);
    GD.PrintErr(
      $"{Name} does not have requested State {typeof(T)} | " +
      $"Requested from {frame.GetMethod().DeclaringType.Name} " +
      $"in method {frame.GetMethod().Name}"
    );
    return null;
  }

  public T? GetStateOrNull<T>(StringName stateName) where T : StateV2 {
    return states.Find((s) => s.name == stateName && s is T) as T;
  }

  public void AddState(StringName stateName, StateV2 state) {
    if(states.Contains(state)) { return; }
    states.Add(state);
    state.name = stateName;
    state.Completed += ChangeState;
    state.Start();

    if(debug) { GD.Print($"State {state.GetType()} added to {Name}"); }
  }

  public void UpdateInput(InputPackage newInput) => input = newInput;

  public void ForceChangeState(StateV2 newState) {
    if(!states.Contains(newState)) {
      GD.PrintErr($"{Name} does not contain {newState.GetType()}");
      return;
    }

    ChangeState(newState);
  }

  public StringName GetCurrentStateName() {
    return currentState == null ? "none" : currentState.name;
  }

  protected abstract void SetupStates();

  protected virtual void ChangeState(StateV2 newState) {
    if(!states.Contains(newState)) {
      GD.PrintErr($"{Name} does not contain {newState.GetType()}");
      return;
    }

    if(newState == currentState) { return; }

    if(!newState.CanEnter()) {
      if(debug) { GD.Print($"{newState.name} could not be entered in {Name}"); }
      return;
    }

    previousState = currentState;
    currentState?.Exit();
    EmitSignalStateExited(currentState);
    if(debug) { GD.Print($"{Name} exited state {currentState.name}"); }

    currentState = newState;
    stateChangeCount++;
    AddToHistory(currentState);

    currentState.Enter();
    EmitSignalStateEntered(currentState);
    if(debug) { GD.Print($"{Name} entered state {currentState.name}"); }

    EmitSignalStateChanged(previousState, currentState);
  }

  protected virtual void ChangeState(StringName newStateName) {
    StateV2? newState = states.Find((s) => s.name == newStateName);
    if(newState == null) {
      GD.PrintErr($"{Name} does not contain {newStateName}");
      return;
    }

    if(newState == currentState) { return; }

    if(!newState.CanEnter()) {
      if(debug) { GD.Print($"{newState.name} could not be entered in {Name}"); }
      return;
    }

    previousState = currentState;
    currentState?.Exit();
    EmitSignalStateExited(currentState);
    if(debug) { GD.Print($"{Name} exited state {currentState.name}"); }

    currentState = newState;
    stateChangeCount++;
    AddToHistory(currentState);

    currentState.Enter();
    EmitSignalStateEntered(currentState);
    if(debug) { GD.Print($"{Name} entered state {currentState.name}"); }

    EmitSignalStateChanged(previousState, currentState);
  }

  protected void AddToHistory(StateV2 state) {
    if(stateHistory.Count >= maxHistory) { stateHistory.RemoveAt(0); }
    stateHistory.Add(state.GetType().ToString());
  }
}

