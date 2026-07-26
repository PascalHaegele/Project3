using Godot;

public partial class MainMenu : Control {
  public override void _Ready() {
    GetNode<Button>("%Start").Pressed += OnStartPressed;
    GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();
  }

  private void OnStartPressed() {
    // _ = GetTree().ChangeSceneToFile("res://maps/test_map.tscn");
    Visible = false;
    Player player =
      ResourceLoader
        .Load<PackedScene>("res://actors/player/player.tscn")
        .Instantiate<Player>();
    GetNode("../World/Actors").AddChild(player);

    EventManager map =
      ResourceLoader
        .Load<PackedScene>("res://maps/test_map.tscn")
        .Instantiate<EventManager>();
    GetNode("../World/Maps").AddChild(map);

    player.GlobalPosition = map.GetNode<Marker3D>("PlayerSpawn").GlobalPosition;
  }
}

