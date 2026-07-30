using Godot;

/// <summary>
/// Debug tool to spawn all pages and print their effects.
/// Attach to any node in the scene.
/// Press P to spawn all pages at once with printed stats.
/// </summary>
[GlobalClass]
public partial class PageDebugTool : Node {
  [Export] private Node? spawnCenter;
  [Export] private PackedScene? pagePickupScene;

  private Node3D? _spawnCenter;

  public override void _Ready() {
    // Validate exported field type safely
    if (spawnCenter is Node3D n3d) {
      _spawnCenter = n3d;
    } else {
      _spawnCenter = null;
    }

    // Try to autofind spawn center near player if not set
    if (_spawnCenter == null) {
      var found = GetTree().Root.FindChild("Player", true, false);
      if (found is Node3D playerNode) {
        _spawnCenter = playerNode;
      } else {
        _spawnCenter = GetParent() as Node3D;
      }
    }
  }

  public override void _Input(InputEvent @event) {
    if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.P) {
      SpawnAllPages();
    }
  }

  /// <summary>
  /// Finds a suitable parent node to spawn pickups into.
  /// </summary>
  private Node GetSpawnParent() {
    if (GetTree() == null || GetTree().Root == null) {
      return this;
    }
    var world = GetTree().Root.GetNodeOrNull<Node>("World");
    if (world != null) {
      return world;
    }
    var testMap = GetTree().Root.GetNodeOrNull<Node3D>("TestMap");
    if (testMap != null) {
      return testMap;
    }
    return GetTree().Root;
  }

  /// <summary>
  /// Spawns every page in the database at random positions around spawnCenter
  /// and prints their full stats to the console.
  /// </summary>
  public void SpawnAllPages() {
    if (PageDatabase.PageCount <= 0) {
      GD.Print("PageDebugTool: No pages in database.");
      return;
    }

    if (_spawnCenter == null) {
      GD.PrintErr("PageDebugTool: No spawnCenter set.");
      return;
    }

    GD.Print("========== PAGE DEBUG SPAWN ==========");
    GD.Print($"Total pages: {PageDatabase.PageCount}");

    // Load pickup scene if not already set
    if (pagePickupScene == null) {
      pagePickupScene = GD.Load<PackedScene>("res://objects/items/test_pickup.tscn");
    }
    if (pagePickupScene == null) {
      GD.PrintErr("PageDebugTool: Could not load test_pickup.tscn");
      return;
    }

    var allPages = new System.Collections.Generic.List<PageData>(PageDatabase.GetPagesByCategory(""));
    int index = 0;
    foreach (PageData page in allPages) {
      // Print stats
      GD.Print($"--- [{page.Rarity}] {page.PageName} ---");
      GD.Print($"  Weapon  : {page.WeaponEffect} (+{page.WeaponEffectValue})");
      GD.Print($"  Armor   : {page.ArmorEffect} (+{page.ArmorEffectValue})");
      GD.Print($"  Skill   : {page.SkillEffect} (+{page.SkillEffectValue})");

      // Spawn pickup
      Pickup pickup = pagePickupScene.Instantiate<Pickup>();
      GetSpawnParent().AddChild(pickup);

      Vector3 pos = _spawnCenter.GlobalPosition;
      float angle = index * (Mathf.Pi * 2f / (float)allPages.Count);
      float radius = 1.5f;
      pos.X += Mathf.Cos(angle) * radius;
      pos.Z += Mathf.Sin(angle) * radius;
      pos.Y += 1.0f;
      pickup.GlobalPosition = pos;

      pickup.itemType = ItemType.PAGE;
      pickup.pageData = page;

      pickup.ApplyImpulse(new Vector3(
        (float)GD.RandRange(-0.3f, 0.3f),
        2.0f,
        (float)GD.RandRange(-0.3f, 0.3f)
      ));

      index++;
    }

    GD.Print("======================================");
  }
}