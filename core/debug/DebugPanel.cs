using Godot;

[GlobalClass]
public partial class DebugPanel : PanelContainer {
  private VBoxContainer propertyContainer;

  private float framesPerSecond;

  public override void _Ready() {
    Debug.panel = this;
    propertyContainer = GetNode<VBoxContainer>("MarginContainer/VBoxContainer");

    Visible = false;
  }

  public override void _Process(double delta) {
    if(!Visible) { return; }

    framesPerSecond = 1.0f / (float)delta;
    AddProperty("FPS", framesPerSecond.ToString("f0"), 0);
  }

  public override void _Input(InputEvent @event) {
    if(@event.IsActionPressed("debug_panel")) { Visible = !Visible; }
  }

  public void AddProperty(string title, string value, int order) {
    Label target = (Label)propertyContainer.FindChild(title, true, false);
    if(target == null) {
      target = new Label();
      target.Name = title;
      target.Text = target.Name + ": " + value;
      propertyContainer.AddChild(target);
    } else if(Visible) {
      target.Text = target.Name + ": " + value;
      propertyContainer.MoveChild(target, order);
    }
  }
}

