using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class SpawnArea : Area3D {
  [Export] private PackedScene enemy;
  [Export] private EnemyInfo enemyInfo;
  private readonly List<Marker3D> spawnPositions = new();

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask = (uint)CollisionLayerEnum.PLAYER;

    Monitorable = false;

    _ = Connect(
      Area3D.SignalName.BodyEntered,
      Callable.From<Node3D>(OnBodyEntered),
      (uint)ConnectFlags.OneShot
    );

    foreach(Node child in GetChildren()) {
      if(child is Marker3D marker) { spawnPositions.Add(marker); }
    }
  }

  private void OnBodyEntered(Node3D body) {
    for(int i = 0; i < spawnPositions.Count; i++) {
      Enemy e = enemy.Instantiate<Enemy>();
      e.enemyInfo = enemyInfo;
      e.enemyInfo.ResourceLocalToScene = true;
      AddChild(e);
      e.Position = spawnPositions[i].Position;
      e.Rotation = spawnPositions[i].Rotation;
    }
  }
}

