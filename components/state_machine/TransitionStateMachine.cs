using Godot;
using System;

[GlobalClass]
public abstract partial class TransitionStateMachine : StateMachine {
  public bool autoUpdate = true;

  private TransitionManager transitionManager = new();

  public override void _Ready() {
    base._Ready();
    SetupTransitions();
  }

  protected abstract void SetupTransitions();

  protected void AddTransition(
    StringName from,
    StringName to,
    Func<bool> condition,
    int priority = 0
  ) {
    _ = transitionManager.AddTransition(from, to, condition, priority);
  }

  protected void AddGlobalTransition(
    StringName to,
    Func<bool> condition,
    int priority = 100
  ) {
    _ = transitionManager.AddGlobalTransition(to, condition, priority);
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if(autoUpdate) { CheckTransitions(); }
  }

  protected override void ChangeState(State newState) {
    StringName oldState = GetCurrentStateName();
    base.ChangeState(newState);

    if(oldState != "none") {
      transitionManager.RecordTransition(oldState, newState.name, "manual");
    }
  }

  protected override void ChangeState(StringName newStateName) {
    StringName oldState = GetCurrentStateName();
    base.ChangeState(newStateName);

    if(oldState != "none") {
      transitionManager.RecordTransition(oldState, newStateName, "manual");
    }
  }

  private void CheckTransitions() {
    Transition? availableTransition =
      transitionManager.GetAvailableTransition(GetCurrentStateName());

    if(availableTransition != null) {
      StringName from = GetCurrentStateName();
      StringName to = availableTransition.to;
      if(transitionManager.ValidateTransition(from, to)) {
        availableTransition.Trigger();
        transitionManager.RecordTransition(from, to, availableTransition.name);
        ChangeState(to);
      }
    }
  }
}

