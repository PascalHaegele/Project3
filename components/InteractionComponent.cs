using Godot;

[GlobalClass]
public partial class InteractionComponent : Area3D {
  private Node3D owner;
  private Player? player;

  private Callable callable;

  public override void _Ready() {
    owner = GetParent<Node3D>();
    callable = Callable.From(TryInteract);
    BodyEntered += OnAreaBodyEntered;
    BodyExited += OnAreaBodyExited;
  }

  private void TryInteract() {
    if(player == null) { return; }
    if(owner is IInteractable interactable) { interactable.Interact(player); }
  }

  private void OnAreaBodyEntered(Node3D body) {
    if(body is not Player) { return; }
    player = body as Player;
    _ = player
      .Connect(
        Player.SignalName.Interacting,
        callable,
        (uint)ConnectFlags.OneShot
      );
  }

  private void OnAreaBodyExited(Node3D body) {
    if(body is not Player) { return; }
    if(player.IsConnected(Player.SignalName.Interacting, callable)) {
      player.Disconnect(Player.SignalName.Interacting, callable);
    }
    player = null;
  }
}

public interface IInteractable {
  void Interact(Player player);
}

