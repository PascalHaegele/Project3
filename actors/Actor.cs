using Godot;
using System.Diagnostics;

[GlobalClass]
public abstract partial class Actor : CharacterBody3D {
  [Export] public VelocityInfo velocityInfo;
  [Export] protected Node[] components;

  public Vector3 Direction { get; protected set; }

  public T? GetComponent<T>() where T : Node {
    foreach(Node component in components) {
      if(component is T) { return component as T; }
    }

    StackFrame frame = new StackTrace().GetFrame(1);
    GD.PrintErr(
      $"{Name} does not have requested Component {typeof(T)} | " +
      $"Requested from {frame.GetMethod().DeclaringType.Name} " +
      $"in method {frame.GetMethod().Name}"
    );
    return null;
  }
}

