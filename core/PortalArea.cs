using Godot;

[GlobalClass]
public partial class PortalArea : Area3D {
  public float chance = 0.0f;

  private PackedScene portal =
    ResourceLoader.Load<PackedScene>("res://objects/portal/portal.tscn");

  private Marker3D portalPosition;
  private Marker3D portalDestination;

  private RandomNumberGenerator rng;

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.NONE;
    CollisionMask = (uint)CollisionLayerEnum.PLAYER;
    Monitorable = false;

    BodyEntered += OnBodyEntered;

    portalPosition = GetNode<Marker3D>("PortalPosition");
    portalDestination = GetNode<Marker3D>("PortalDestination");

    rng = new();
    rng.Randomize();
  }

  private void OnBodyEntered(Node3D body) {
    if(rng.Randf() >= chance) { return; }

    Portal p = portal.Instantiate<Portal>();
    AddChild(p);
    p.GlobalPosition = portalPosition.GlobalPosition;
    p.GlobalRotation = portalPosition.GlobalRotation;
    p.destination = portalDestination;

    BodyEntered -= OnBodyEntered;
  }
}

