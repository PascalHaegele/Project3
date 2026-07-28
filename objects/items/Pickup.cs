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

  // Preload currency GLB models
  private static readonly PackedScene Currency1Model = GD.Load<PackedScene>("res://objects/items/currency/Currency1.glb");
  private static readonly PackedScene Currency2Model = GD.Load<PackedScene>("res://objects/items/currency/Currency2.glb");
  private static readonly PackedScene Currency3Model = GD.Load<PackedScene>("res://objects/items/currency/Currency3.glb");

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

      // Currency pickups spawn their model dynamically
      if (itemType == ItemType.CURRENCY1 || itemType == ItemType.CURRENCY2 || itemType == ItemType.CURRENCY3) {
        SpawnCurrencyModel();
      }
    }).CallDeferred();
  }

  private void SpawnCurrencyModel() {
    PackedScene modelScene = itemType switch {
      ItemType.CURRENCY1 => Currency1Model,
      ItemType.CURRENCY2 => Currency2Model,
      ItemType.CURRENCY3 => Currency3Model,
      _ => null
    };

    if (modelScene != null) {
      Node3D model = modelScene.Instantiate<Node3D>();
      model.Scale = new Vector3(0.3f, 0.3f, 0.3f);
      AddChild(model);
    }
  }

  public override void _PhysicsProcess(double delta) {
    hoverIndicator.Visible = hovering;
    if(hovering) { RotateY(0.05f); }
  }

  private void OnBodyEntered(Node body) {
    if(body is not Chest) { Freeze = true; }
  }
}
