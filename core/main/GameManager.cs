using Godot;

public partial class GameManager : Node {
  private EventManager eventManager;
  private MainMenu mainMenu;
  private PauseMenu pauseMenu;

  private Player? player;

  private Node3D currentMap;

  public override void _Ready() {
    eventManager = GetNode<EventManager>("Systems/EventManager");
    eventManager.PortalLevelChange += LoadNewMap;
    eventManager.Restart += OnRestart;

    mainMenu = GetNode<MainMenu>("Menus/MainMenu");
    mainMenu.Start += (mapPath) => {
      LoadPlayer();
      HideMainMenu();
      LoadNewMap(mapPath);
    };

    pauseMenu = GetNode<PauseMenu>("Menus/PauseMenu");
    pauseMenu.ProcessMode = ProcessModeEnum.WhenPaused;

    pauseMenu.Resume += HidePauseMenu;
    pauseMenu.QuitToMenu += () => {
      HidePauseMenu();
      OnRestart();
    };

    pauseMenu.ChangeMouseSense +=
      (value) => player.GetComponent<CameraComponent>().Sensitivity = value;

    ShowMainMenu();
  }

  public override void _UnhandledInput(InputEvent @event) {
    if(Input.IsActionJustPressed("exit")) {
      if(pauseMenu.Visible) { HidePauseMenu(); }
      else { ShowPauseMenu(); }
    }

    if(Input.IsActionJustPressed("mouse_capture")) {
      Input.MouseMode =
        Input.MouseMode == Input.MouseModeEnum.Captured ?
        Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }
  }

  private void OnRestart() {
    player.QueueFree();
    ShowMainMenu();
  }

  private void LoadPlayer() {
    player = ResourceLoader
    .Load<PackedScene>("res://actors/player/player.tscn")
    .Instantiate<Player>();

    GetNode<Node3D>("World/Actors").AddChild(player);
    eventManager.SetPlayer(player);

    pauseMenu.SetMouseSense(player.GetComponent<CameraComponent>().Sensitivity);
  }

  private void ShowMainMenu() {
    Input.MouseMode = Input.MouseModeEnum.Visible;
    mainMenu.ProcessMode = ProcessModeEnum.Always;
    mainMenu.Show();
    if(player != null) {
      player.HideHUD();
      player.Hide();
      player.ProcessMode = ProcessModeEnum.Disabled;
    }
    GetTree().Paused = true;
  }

  private void HideMainMenu() {
    GetTree().Paused = false;
    mainMenu.Hide();
    mainMenu.ProcessMode = ProcessModeEnum.Disabled;
    if(player != null) {
      player.Show();
      player.ShowHUD();
      player.ProcessMode = ProcessModeEnum.Inherit;
    }
    Input.MouseMode = Input.MouseModeEnum.Captured;
  }

  private void LoadNewMap(StringName path) {
    if(path.Equals("res://maps/test_map.tscn")) { player?.Reset(); }

    Node3D oldMap = currentMap;
    PackedScene? packed = ResourceLoader.Load<PackedScene>(path);
    if(packed == null) {
      GD.PrintErr($"Failed to load map: {path}");
      return;
    }

    Node3D map = packed.Instantiate<Node3D>();
    currentMap = eventManager.CurrentMap = map;
    GetNode<Node3D>("World/Maps").AddChild(map);

    if(player == null) {
      player = FindChild("Player") as Player;
      GetNode<Node3D>("World/Actors").AddChild(player);
      eventManager.SetPlayer(player);
    }

    Marker3D? playerSpawn = map.GetNodeOrNull<Marker3D>("PlayerSpawn");
    if(playerSpawn != null && player != null) {
      player.GlobalPosition = playerSpawn.GlobalPosition;
      player.GlobalRotation = playerSpawn.GlobalRotation;
    } else {
      GD.PrintErr("PlayerSpawn not found on map or player is null");
    }

    oldMap?.QueueFree();
  }

  private void ShowPauseMenu() {
    Input.MouseMode = Input.MouseModeEnum.Visible;
    pauseMenu.Show();
    if(player != null) {
      player.HideHUD();
      player.Hide();
    }
    GetTree().Paused = true;
  }

  private void HidePauseMenu() {
    GetTree().Paused = false;
    if(player != null) {
      player.Show();
      player.ShowHUD();
    }
    pauseMenu.Hide();
    Input.MouseMode = Input.MouseModeEnum.Captured;
  }
}

