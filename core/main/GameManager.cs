using Godot;

public partial class GameManager : Node {
  private EventManager eventManager;
  private MainMenu mainMenu;

  [Export] private bool createPlayer = true;
  private Player? player;

  private Node3D currentMap;

  public override void _Ready() {
    eventManager = GetNode<EventManager>("Systems/EventManager");
    eventManager.PortalLevelChange += LoadNewMap;

    mainMenu = GetNode<MainMenu>("MainMenu");
    mainMenu.Start += (mapPath) => { HideMainMenu(); LoadNewMap(mapPath); };

    if(createPlayer) {
      player =
        ResourceLoader
        .Load<PackedScene>("res://actors/player/player.tscn")
        .Instantiate<Player>();
      GetNode<Node3D>("World/Actors").AddChild(player);
      eventManager.SetPlayer(player);
    }

    ShowMainMenu();
  }

  public override void _UnhandledInput(InputEvent @event) {
    if(Input.IsActionJustPressed("exit")) { GetTree().Quit(); }

    if(Input.IsActionJustPressed("mouse_capture")) {
      Input.MouseMode =
        Input.MouseMode == Input.MouseModeEnum.Captured ?
        Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }
  }

  private void ShowMainMenu() {
    Input.MouseMode = Input.MouseModeEnum.Visible;
    mainMenu.ProcessMode = ProcessModeEnum.Always;
    mainMenu.Show();
    player.HideHUD();
    player.Hide();
    player.ProcessMode = ProcessModeEnum.Disabled;
    GetTree().Paused = true;
  }

  private void HideMainMenu() {
    GetTree().Paused = false;
    mainMenu.Hide();
    mainMenu.ProcessMode = ProcessModeEnum.Disabled;
    player.Show();
    player.ShowHUD();
    player.ProcessMode = ProcessModeEnum.Inherit;
    Input.MouseMode = Input.MouseModeEnum.Captured;
  }

  private void LoadNewMap(StringName path) {
    Node3D oldMap = currentMap;
    Node3D map = ResourceLoader.Load<PackedScene>(path).Instantiate<Node3D>();
    currentMap = eventManager.CurrentMap = map;
    GetNode<Node3D>("World/Maps").AddChild(map);

    if(player == null) {
      player = FindChild("Player") as Player;
      GetNode<Node3D>("World/Actors").AddChild(player);
      eventManager.SetPlayer(player);
    }

    Marker3D playerSpawn = map.GetNode<Marker3D>("PlayerSpawn");

    player.GlobalPosition = playerSpawn.GlobalPosition;
    player.GlobalRotation = playerSpawn.GlobalRotation;

    oldMap?.QueueFree();
  }
}

