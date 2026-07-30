using Godot;

[GlobalClass]
public partial class InteractionComponent : Area3D {
  private Node3D owner;

  [Export] private ShaderMaterial? outlineMaterial;
  [Export] private Node3D? ownerModel;

  private Player? player;

  private Sprite3D? interactableIndicator;

  private Callable callable;

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask = (uint)CollisionLayerEnum.PLAYER;
    SetDeferred(Area3D.PropertyName.Monitorable, false);

    owner = GetParent<Node3D>();

    interactableIndicator = GetNodeOrNull<Sprite3D>("InteractableIndicator");
    interactableIndicator?.Hide();

    callable = Callable.From(TryInteract);

    BodyEntered += OnAreaBodyEntered;
    BodyExited += OnAreaBodyExited;

    if(outlineMaterial != null && ownerModel != null) {
      foreach(Node child in ownerModel.GetChildren()) {
        if(child is MeshInstance3D mesh) {
          if(mesh.Mesh is ArrayMesh arrayMesh) {
            Material mat = arrayMesh.SurfaceGetMaterial(0);
            mat.NextPass = outlineMaterial;
          }
          if(mesh.Mesh is PrimitiveMesh primitiveMesh) {
            primitiveMesh.Material ??= new StandardMaterial3D();
            primitiveMesh.Material.NextPass = outlineMaterial;
          }
        }
      }
    }
  }

  private void TryInteract() {
    if(player == null) { return; }
    if(owner is IInteractable interactable) {
      interactable.Interact(player);
      GetChild<CollisionShape3D>(0).Disabled = true;
    }
  }

  private void OnAreaBodyEntered(Node3D body) {
    if(body is not Player) { return; }

    interactableIndicator?.Show();

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

    interactableIndicator?.Hide();

    if(player.IsConnected(Player.SignalName.Interacting, callable)) {
      player.Disconnect(Player.SignalName.Interacting, callable);
    }

    player = null;
  }
}

public interface IInteractable {
  void Interact(Player player);
}

