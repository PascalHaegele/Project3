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

  public int amount;

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

    // Add a small box collision shape to prevent falling through floor
    CollisionShape3D floorCollision = new();
    BoxShape3D box = new();
    box.Size = new Vector3(0.3f, 0.1f, 0.3f);
    floorCollision.Shape = box;
    AddChild(floorCollision);

    // Show the correct mesh; defer once to avoid transform issues
    Callable.From(() => {
      GetNodeOrNull<Node3D>("Potion")?
        .Set("visible", itemType == ItemType.POTION);
      GetNodeOrNull<Node3D>("Page")?
        .Set("visible", itemType == ItemType.PAGE);
      GetNodeOrNull<Node3D>("AmmoRev")?
        .Set("visible", itemType == ItemType.R_AMMO);
      GetNodeOrNull<Node3D>("AmmoShot")?
        .Set("visible", itemType == ItemType.S_AMMO);
    }).CallDeferred();

    ApplyImpulse(new Vector3(0.0f, 3.0f, 2.0f));
  }

  public override void _PhysicsProcess(double delta) {
    hoverIndicator.Visible = hovering;
    if(hovering) { RotateY(0.05f); }
  }

  private void OnBodyEntered(Node body) {
    if(body is not Chest) { Freeze = true; }
  }
}

