using Godot;
using Godot.Collections;

[Tool, GlobalClass]
public partial class PatrolPathConverter : EditorScript {
  private EditorInterface ei;
  private Window window;
  private readonly Array<Vector3> patrolPoints = [];
  private Vector3 leashPoint = new();
  private float leashLength;
  private float attackRange;

  public override void _Run() {
    ei = EditorInterface.Singleton;
    if(ei == null) { return; }

    window = new Window();
    window.CloseRequested += window.QueueFree;
    window.Size = new Vector2I(640, 480);

    VBoxContainer vBox = new();
    window.AddChild(vBox);

    Button closeButton = new();
    closeButton.Text = "Close";
    closeButton.Pressed += () => { window.QueueFree(); };
    vBox.AddChild(closeButton);

    Button patrolButton = new();
    patrolButton.Text = "Patrol";
    patrolButton.Pressed += () => {
      if(ei == null) { return; }
      ei.PopupNodeSelector(
        Callable.From<NodePath>(OnPatrolSelected),
        [nameof(Node3D)]
      );
    };
    vBox.AddChild(patrolButton);

    Button leashButton = new();
    leashButton.Text = "Leash Point";
    leashButton.Pressed += () => {
      if(ei == null) { return; }
      ei.PopupNodeSelector(
        Callable.From<NodePath>(OnLeashSelected),
        [nameof(Marker3D)]
      );
    };
    vBox.AddChild(leashButton);

    Button enemyButton = new();
    enemyButton.Text = "Enemy";
    enemyButton.Pressed += () => {
      if(ei == null) { return; }
      ei.PopupNodeSelector(
        Callable.From<NodePath>(OnEnemySelected),
        [nameof(Node)]
      );
    };
    vBox.AddChild(enemyButton);

    HBoxContainer leashLengthBox = new();
    vBox.AddChild(leashLengthBox);

    LineEdit leashLengthEdit = new();
    leashLengthBox.AddChild(leashLengthEdit);

    Button leashLengthButton = new();
    leashLengthBox.AddChild(leashLengthButton);
    leashLengthButton.Text = "Submit";

    ei.PopupDialog(window, new Rect2I(100, 100, window.Size.X, window.Size.Y));

    // patrolButton.Position = new Vector2I(window.Size.X / 2, 20);
    // leashButton.Position =
    //   new Vector2I(window.Size.X / 2, (int)(patrolButton.Position.Y + 100));
    // enemyButton.Position =
    //   new Vector2I(window.Size.X / 2, (int)(leashButton.Position.Y + 100));
    // closeButton.Position =
    //   new Vector2I(window.Size.X / 2, (int)(enemyButton.Position.Y + 100));
  }

  private void OnPatrolSelected(NodePath nodePath) {
    if(ei?.GetEditedSceneRoot() is not Node editedRoot) { return; }
    if(editedRoot.GetNodeOrNull<Node3D>(nodePath) is not Node3D patrolRoot) { return; }

    patrolPoints.Clear();
    foreach(Node child in patrolRoot.GetChildren()) {
      if(child is Marker3D marker) {
        patrolPoints.Add(marker.GlobalPosition);
      }
    }
  }

  private void OnLeashSelected(NodePath nodePath) {
    if(ei?.GetEditedSceneRoot() is not Node editedRoot) { return; }
    if(editedRoot.GetNodeOrNull<Marker3D>(nodePath) is not Marker3D leash) { return; }
    leashPoint = leash.GlobalPosition;
  }

  private void OnEnemySelected(NodePath nodePath) {
    if(ei?.GetEditedSceneRoot() is not Node editedRoot) { return; }
    Node? enemy = editedRoot.GetNodeOrNull<Node>(nodePath);
    if(enemy == null) { return; }

    Variant enemyInfoVariant = enemy.Get(Enemy.PropertyName.enemyInfo);
    if(enemyInfoVariant.VariantType == Variant.Type.Object && enemyInfoVariant.AsGodotObject() is EnemyInfo enemyInfo) {
      enemyInfo.patrolPath = [.. patrolPoints];
      enemyInfo.leashPoint = leashPoint;
    }
  }
}

