using Godot;

public partial class TestInteractable : StaticBody3D, IInteractable {
  public void Interact() {
    GD.Print($"Interaction with {Name}");
  }
}

