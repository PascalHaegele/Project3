using Godot;

public partial class PauseMenu : Control {
  private PanelContainer settingsPanel;
  private PanelContainer guidePanel;

  private Slider mouseSenseSlider;
  private Label mouseSenseDisplay;

  [Signal] public delegate void ResumeEventHandler();
  [Signal] public delegate void QuitToMenuEventHandler();

  [Signal] public delegate void ChangeMouseSenseEventHandler(float value);

  public override void _Ready() {
    settingsPanel = GetNode<PanelContainer>("SettingsPanel");
    settingsPanel.Hide();

    guidePanel = GetNode<PanelContainer>("GuidePanel");
    guidePanel.Hide();

    GetNode<Button>("%Resume").Pressed += EmitSignalResume;
    GetNode<Button>("%Settings").Pressed += () => settingsPanel.Show();
    GetNode<Button>("%Guide").Pressed += () => guidePanel.Show();
    GetNode<Button>("%QuitToMenu").Pressed += EmitSignalQuitToMenu;
    GetNode<Button>("%QuitGame").Pressed += () => GetTree().Quit();

    mouseSenseSlider = GetNode<Slider>("%MouseSenseSlider");
    mouseSenseDisplay = GetNode<Label>("%MouseSenseDisplay");

    mouseSenseSlider.ValueChanged +=
      (value) => mouseSenseDisplay.Text = value.ToString("f2");

    GetNode<Button>("%CloseSettings").Pressed += () => {
      EmitSignalChangeMouseSense((float)mouseSenseSlider.Value);
      settingsPanel.Hide();
    };

    GetNode<Button>("%CloseGuide").Pressed += () => guidePanel.Hide();
  }

  public void SetMouseSense(float value) {
    mouseSenseSlider.Value = value;
    mouseSenseDisplay.Text = value.ToString("f2");
  }
}

