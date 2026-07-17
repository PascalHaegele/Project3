using Godot;
using System.Collections.Generic;

[GlobalClass]
public abstract partial class State : RefCounted {
  public float timeEntered;
  public StringName name = "";

  protected StateMachine stateMachine;

  protected Dictionary<string, object> data = new();

  public float TimeInState { get; private set; }

  [Signal] public delegate void CompletedEventHandler(State newState);

  public virtual bool CanEnter() => true;

  public virtual void Start() { }

  public virtual void Enter() {
    timeEntered = Time.GetTicksMsec() / 1000.0f;
    TimeInState = 0.0f;
  }

  public virtual void Update(double delta) => TimeInState += (float)delta;

  public virtual void PhysicsUpdate(double delta) { }

  public virtual void Exit() { }

  public string AsString() {
    return $"State {GetClass()} - Time: {TimeInState:f2}";
  }

  protected void ChangeTo<T>() where T : State {
    State? state = stateMachine.GetStateOrNull<T>();
    if(state != null) { EmitSignalCompleted(state); }
  }

  protected void SetData(string key, object value) {
    if(!data.TryAdd(key, value)) { data[key] = value; }
  }

  protected object GetData(string key) {
    return data.TryGetValue(key, out object value) ? value : null;
  }
}

