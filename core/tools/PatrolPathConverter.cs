using Godot;
using Godot.Collections;

[Tool, GlobalClass]
public partial class PatrolPathConverter : EditorScript {
  private EditorInterface ei;

  private Window window;

  private Array<Vector3>patrolPoints = [];

  private Vector3 leashPoint = new();

  public override void _Run() {
    ei = EditorInterface.Singleton;

    window = new();
    window.CloseRequested += window.QueueFree;

    window.Size = new(640, 480);

    Button closeButton = new();
    closeButton.Text = "close";
    closeButton.Pressed += window.QueueFree;

    window.AddChild(closeButton);

    Button patrolButton = new();
    patrolButton.Text = "patrol";
    patrolButton.Pressed +=
      () => ei
        .PopupNodeSelector(
          Callable.From<NodePath>(OnPatrolSelected),
          [nameof(Node3D)]
        );

    window.AddChild(patrolButton);

    Button leashButton = new();
    leashButton.Text = "leash";
    leashButton.Pressed +=
      () => ei
        .PopupNodeSelector(
          Callable.From<NodePath>(OnLeashSelected),
          [nameof(Marker3D)]
        );

    window.AddChild(leashButton);

    Button enemyButton = new();
    enemyButton.Text = "enemy";
    enemyButton.Pressed +=
      () => ei
        .PopupNodeSelector(
          Callable.From<NodePath>(OnEnemySelected),
          [nameof(Enemy)]
        );

    window.AddChild(enemyButton);

    ei.PopupDialog(window, new(100, 100, window.Size));

    patrolButton.Position =
      new Vector2(
        window.Size.X / 2,
        20
      );

    leashButton.Position =
      new Vector2(
        window.Size.X / 2,
        patrolButton.Position.Y + 100
      );

    enemyButton.Position =
      new Vector2(
        window.Size.X / 2,
        leashButton.Position.Y + 100
      );

    closeButton.Position =
      new Vector2(
        window.Size.X / 2,
        enemyButton.Position.Y + 100
      );
  }

  private void OnPatrolSelected(NodePath nodePath) {
    Node3D patrolRoot = ei.GetEditedSceneRoot().GetNode<Node3D>(nodePath);
    foreach(Node child in patrolRoot.GetChildren()) {
      if(child is Marker3D marker) {
        patrolPoints.Add(marker.GlobalPosition);
      }
    }
  }

  private void OnLeashSelected(NodePath nodePath) {
    Marker3D leash= ei.GetEditedSceneRoot().GetNode<Marker3D>(nodePath);
    leashPoint = leash.GlobalPosition;
  }

  private void OnEnemySelected(NodePath nodePath) {
    Node enemy = ei.GetEditedSceneRoot().GetNode(nodePath);

    EnemyInfo enemyInfo = (EnemyInfo)enemy.Get(Enemy.PropertyName.info);
    enemyInfo.patrolPath = [.. patrolPoints];
    enemyInfo.leashPoint = leashPoint;
  }
}

