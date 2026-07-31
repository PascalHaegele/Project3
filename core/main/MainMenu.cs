using Godot;

public partial class MainMenu : Control {
  [Export(PropertyHint.File, "*.tscn")] private string startMapPath;

  [Signal] public delegate void StartEventHandler(StringName mapPath);

  public override void _Ready() {
    GetNode<Button>("%Start").Pressed += () => EmitSignalStart(startMapPath);
    GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();
  }
}

