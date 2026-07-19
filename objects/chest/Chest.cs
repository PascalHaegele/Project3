using Godot;

public partial class Chest : StaticBody3D, IInteractable {
  [Export] private PackedScene pickup;

  private AnimationPlayer animationPlayer;
  private bool opened = false;

  public override void _Ready() {
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
  }

  public void Interact(Player player) {
    if (opened) return;
    opened = true;

    animationPlayer.Play("open");
    GD.Print($"Chest.Interact: {Name} opened");

    if (pickup == null) {
      GD.Print("Chest: No pickup prefab configured.");
      return;
    }

    // Spawn pickup at world position above the chest
    Pickup p = pickup.Instantiate<Pickup>();
    GetParent().AddChild(p);
    p.GlobalPosition = GlobalPosition + new Vector3(0.0f, 1.5f, 0.0f);
    p.ApplyImpulse(new Vector3(
      (float)GD.RandRange(-1.0f, 1.0f),
      3.0f,
      (float)GD.RandRange(-1.0f, 1.0f)
    ));
    
    // Try to generate a page drop
    PageData page = GeneratePageDrop();
    if (page != null) {
      p.itemType = ItemType.PAGE;
      p.pageData = page;
      GD.Print($"Chest dropped page: {page.PageName} [{page.Rarity}]");
      return;
    }

    // Legacy fallback: just spawn the pickup
    GD.Print($"Chest dropped pickup (legacy fallback)");
  }

  /// <summary>
  /// Generates a random page from the database.
  /// </summary>
  private PageData GeneratePageDrop() {
    if (PageDatabase.PageCount <= 0) return null;

    System.Collections.Generic.List<string> allPages = PageDatabase.GetAllPageNames();
    if (allPages.Count <= 0) return null;

    int randomIndex = (int)(GD.Randi() % (ulong)allPages.Count);
    return PageDatabase.GetPage(allPages[randomIndex]);
  }
}