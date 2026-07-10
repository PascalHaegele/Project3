using Godot;

[GlobalClass]
public partial class SpawnArea : Area3D {
  [Export] public PackedScene enemy;
  [Export] public Marker3D[] spawnPositions;

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask = (uint)CollisionLayerEnum.PLAYER;

    _ = Connect(
      Area3D.SignalName.BodyEntered,
      Callable.From<Node3D>(OnBodyEntered),
      (uint)ConnectFlags.OneShot
    );
  }

  private void OnBodyEntered(Node3D body) {
    for(int i = 0; i < spawnPositions.Length; i++) {
      Enemy e = enemy.Instantiate<Enemy>();
      AddChild(e);
      e.Position = spawnPositions[i].Position;
      e.Rotation = spawnPositions[i].Rotation;
    }
  }
}

