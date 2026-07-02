using Godot;

[GlobalClass]
public partial class InteractionComponent : Area3D {
  [Export]
  private Node3D owner;

  private Player? player;

  public override void _Ready() {
    BodyEntered += OnAreaBodyEntered;
    BodyExited += OnAreaBodyExited;
  }

  private void TryInteract() {
    if(player == null) { return; }
    player.healthComponent.TakeDamage(10.0f);
    if(owner is IInteractable interactable) { interactable.Interact(); }
  }

  private void OnAreaBodyEntered(Node3D body) {
    if(body is not Player) { return; }
    player = body as Player;
    _ = player
      .Connect(
        Player.SignalName.Interacting,
        Callable.From(TryInteract),
        (uint)ConnectFlags.OneShot
      );
  }

  private void OnAreaBodyExited(Node3D body) {
    if(body is not Player) { return; }
    player = null;
  }
}

public interface IInteractable {
  void Interact();
}

