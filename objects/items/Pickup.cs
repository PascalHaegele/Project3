using Godot;

/// <summary>
/// A pickupable item in the world. Can represent ammo, potions, or pages.
/// Pages carry a PageData reference that gets added to InventoryComponent.
/// </summary>
[GlobalClass]
public partial class Pickup : RigidBody3D {
  [Export] public ItemType itemType;

  /// <summary>
  /// If this pickup represents a page, this holds the page data.
  /// Set this when spawning a page pickup.
  /// </summary>
  public PageData pageData;

  public bool hovering;

  private Area3D hoverArea;
  private Sprite3D hoverIndicator;

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask = (uint)CollisionLayerEnum.WORLD;

    BodyEntered += OnBodyEntered;

    hoverArea = GetNode<Area3D>("HoverArea");
    hoverArea.Monitoring = false;
    hoverArea.CollisionLayer = (uint)CollisionLayerEnum.PICKUP;
    hoverArea.CollisionMask = (uint)CollisionLayerEnum.NONE;

    hoverIndicator = GetNode<Sprite3D>("HoverIndicator");
  }

  public override void _PhysicsProcess(double delta) {
    hoverIndicator.Visible = hovering;
    if(hovering) { RotateY(0.05f); }
  }

  private void OnBodyEntered(Node body) {
    if(body is not Chest) { Freeze = true; }
  }
}

