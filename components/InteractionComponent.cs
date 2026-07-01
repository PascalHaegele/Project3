using Godot;

[GlobalClass]
public partial class InteractionComponent : Node {
  [Export]
  public Actor actor;

  [Export]
  private Area3D interactionArea;
  private bool playerInsideArea;

  public override void _Ready() {
    if(interactionArea != null) {
      interactionArea.BodyEntered += OnAreaBodyEntered;
      interactionArea.BodyExited += OnAreaBodyExited;
    } else { GD.PrintErr("no interaction area found"); }
  }

  private void TryInteract() {
    if(interactionArea == null) { return; }

    if(!playerInsideArea) { return; }

    if(interactionArea is IInteractable interactable) {
      interactable.Interact();
    }
  }

  private void OnAreaBodyEntered(Node body) {
    if(actor == null) { return; }

    if(body == actor) { playerInsideArea = true; }
  }

  private void OnAreaBodyExited(Node body) {
    if(actor == null) { return; }

    if(body == actor) { playerInsideArea = false; }
  }
}

public interface IInteractable {
  void Interact();
}

