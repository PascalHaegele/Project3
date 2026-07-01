using Godot;

[GlobalClass]
public abstract partial class Actor : CharacterBody3D {
  [Export]
  protected Node[] components = [];

  public Node GetComponent(System.Type request) {
    foreach(Node component in components) {
      if(component.GetType() == request) { return component; }
    }
    GD.PrintErr(Name + " does not have requested Component");
    return null;
  }
}

