using Godot;

/// <summary>
/// A pickupable item in the world. Can represent ammo, potions, pages, or currencies.
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

  public int amount = 1;

  private Area3D hoverArea;
  private Sprite3D hoverIndicator;

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask = (uint)CollisionLayerEnum.WORLD;

    GravityScale = 2.0f;
    ContactMonitor = true;
    MaxContactsReported = 1;

    BodyEntered += OnBodyEntered;

    hoverArea = GetNode<Area3D>("HoverArea");
    hoverArea.Monitoring = false;
    hoverArea.CollisionLayer = (uint)CollisionLayerEnum.PICKUP;
    hoverArea.CollisionMask = (uint)CollisionLayerEnum.NONE;

    hoverIndicator = GetNode<Sprite3D>("HoverIndicator");

    // Add a small box collision shape to prevent falling through floor
    CollisionShape3D floorCollision = new();
    BoxShape3D box = new();
    box.Size = new Vector3(0.3f, 0.1f, 0.3f);
    floorCollision.Shape = box;
    AddChild(floorCollision);
  }

  public override void _PhysicsProcess(double delta) {
    hoverIndicator.Visible = hovering;
    if(hovering) { RotateY(0.05f); }
  }

  private void OnBodyEntered(Node body) {
    if(body is not Chest) { Freeze = true; }
  }
}
