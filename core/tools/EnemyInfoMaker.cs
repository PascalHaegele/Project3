using Godot;
using Godot.Collections;

[Tool, GlobalClass]
public partial class EnemyInfoMaker : EditorScript {
  private EditorInterface ei;
  private Window window;
  private readonly Array<Vector3> patrolPoints = [];
  private Vector3 leashPoint = new();
  private float leashLength;
  private float minAttackRange;
  private float maxAttackRange;
  private string infoName = "";

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

    HBoxContainer leashLengthBox = new();
    vBox.AddChild(leashLengthBox);

    Label leashLengthLabel = new();
    leashLengthBox.AddChild(leashLengthLabel);
    leashLengthLabel.Text = "Leash Length";

    LineEdit leashLengthEdit = new();
    leashLengthBox.AddChild(leashLengthEdit);

    Button leashLengthButton = new();
    leashLengthBox.AddChild(leashLengthButton);
    leashLengthButton.Text = "Submit";
    leashLengthButton.Pressed += () =>
      leashLength = leashLengthEdit.Text.ToFloat();

    HBoxContainer minAttackRangeBox = new();
    vBox.AddChild(minAttackRangeBox);

    Label minAttackRangeLabel = new();
    minAttackRangeBox.AddChild(minAttackRangeLabel);
    minAttackRangeLabel.Text = "Min Attack Range";

    LineEdit minAttackRangeEdit = new();
    minAttackRangeBox.AddChild(minAttackRangeEdit);

    Button minAttackRangeButton = new();
    minAttackRangeBox.AddChild(minAttackRangeButton);
    minAttackRangeButton.Text = "Submit";
    minAttackRangeButton.Pressed += () =>
      minAttackRange = minAttackRangeEdit.Text.ToFloat();

    HBoxContainer maxAttackRangeBox = new();
    vBox.AddChild(maxAttackRangeBox);

    Label maxAttackRangeLabel = new();
    maxAttackRangeBox.AddChild(maxAttackRangeLabel);
    maxAttackRangeLabel.Text = "Max Attack Range";

    LineEdit maxAttackRangeEdit = new();
    maxAttackRangeBox.AddChild(maxAttackRangeEdit);

    Button maxAttackRangeButton = new();
    maxAttackRangeBox.AddChild(maxAttackRangeButton);
    maxAttackRangeButton.Text = "Submit";
    maxAttackRangeButton.Pressed += () =>
      maxAttackRange = maxAttackRangeEdit.Text.ToFloat();

    HBoxContainer nameBox = new();
    vBox.AddChild(nameBox);

    Label nameLabel = new();
    nameBox.AddChild(nameLabel);
    nameLabel .Text = "Name";

    LineEdit nameEdit = new();
    nameBox.AddChild(nameEdit);

    Button nameButton = new();
    nameBox.AddChild(nameButton);
    nameButton.Text = "Submit";
    nameButton.Pressed += () =>
      infoName = nameEdit.Text;

    Button saveButton = new();
    vBox.AddChild(saveButton);
    saveButton.Text = "Save";
    saveButton.Pressed += () => {
      EnemyInfo info = new();
      info.ResourceLocalToScene = true;
      info.patrolPath = [.. patrolPoints];
      info.leashPoint = leashPoint;
      info.leashLength = leashLength;
      info.minAttackRange = minAttackRange;
      info.maxAttackRange = maxAttackRange;
      _ = ResourceSaver
        .Save(info, "res://actors/enemies/infos/" + infoName + ".res");
      window.QueueFree();
    };

    ei.PopupDialog(window, new Rect2I(100, 100, window.Size.X, window.Size.Y));
  }

  private void OnPatrolSelected(NodePath nodePath) {
    if(ei.GetEditedSceneRoot() is not Node editedRoot) { return; }
    if(editedRoot.GetNodeOrNull<Node3D>(nodePath) is not Node3D patrolRoot) {
      return;
    }

    patrolPoints.Clear();
    foreach(Node child in patrolRoot.GetChildren()) {
      if(child is Marker3D marker) {
        patrolPoints.Add(marker.GlobalPosition);
      }
    }
  }

  private void OnLeashSelected(NodePath nodePath) {
    if(ei.GetEditedSceneRoot() is not Node editedRoot) { return; }
    if(editedRoot.GetNodeOrNull<Marker3D>(nodePath) is not Marker3D leash) {
      return;
    }
    leashPoint = leash.GlobalPosition;
  }
}

