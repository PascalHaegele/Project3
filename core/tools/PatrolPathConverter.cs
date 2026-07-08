using Godot;
using Godot.Collections;

[Tool, GlobalClass]
public partial class PatrolPathConverter : EditorScript {
  private EditorInterface ei;

  private Window window;

  private Array<Vector3>positions = [];

  public override void _Run() {
    ei = EditorInterface.Singleton;

    window = new();
    window.CloseRequested += window.QueueFree;

    window.Size = new(640, 480);

    Button closeButton = new();
    closeButton.Text = "close";
    closeButton.Pressed +=
      () => window.EmitSignal(Window.SignalName.CloseRequested);

    window.AddChild(closeButton);

    Button selectorButton = new();
    selectorButton.Text = "select node";
    selectorButton.Pressed +=
      () => ei
        .PopupNodeSelector(
          Callable.From<NodePath>(OnPatrolRootSelected),
          [nameof(Node3D)]
        );

    window.AddChild(selectorButton);

    ei.PopupDialog(window, new(100, 100, window.Size));

    closeButton.Position =
      new Vector2(
        window.Size.X / 2 - closeButton.Size.X,
        window.Size.Y - closeButton.Size.Y
      );

    selectorButton.Position =
      new Vector2(
        window.Size.X / 2 + selectorButton.Size.X,
        window.Size.Y - selectorButton.Size.Y
      );
  }

  private void OnPatrolRootSelected(NodePath nodePath) {
    Node3D patrolRoot = ei.GetEditedSceneRoot().GetNode<Node3D>(nodePath);
    foreach(Node child in patrolRoot.GetChildren()) {
      if(child is Marker3D marker) {
        positions.Add(marker.GlobalPosition);
      }
    }

    ei.PopupNodeSelector(
      Callable.From<NodePath>(OnEnemySelected),
      [nameof(Enemy)]
    );
  }

  private void OnEnemySelected(NodePath nodePath) {
    var n = ei.GetEditedSceneRoot().GetNode(nodePath);
    var script = (CSharpScript)n.GetScript();

    GD.Print($"runtime={n.GetType().FullName}");
    GD.Print($"scriptPath={script.ResourcePath}");
    GD.Print($"scriptClass={script.GetClass()}");
    GD.Print($"typeof(TestEnemy)={typeof(TestEnemy).FullName}");
    GD.Print($"sameType={ReferenceEquals(n.GetType(), typeof(TestEnemy))}");
    GD.Print($"isTestEnemy={n is TestEnemy}");
    // CharacterBody3D body =
    //   ei.GetEditedSceneRoot().GetNode<CharacterBody3D>(nodePath);
    // if(body is TestEnemy enemy) {
    //   GD.Print(enemy);
    // }
    // enemy.info.patrolPath = new Vector3[positions.Count];
    // positions.CopyTo(enemy.info.patrolPath, 0);
  }
}

