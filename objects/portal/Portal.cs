using Godot;

public partial class Portal : StaticBody3D {
  [Export] public Marker3D destination;

  private Area3D portalArea;

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.WORLD;
    CollisionMask = (uint)CollisionLayerEnum.NONE;

    portalArea = GetNode<Area3D>("PortalArea");
    portalArea.CollisionLayer = (uint)CollisionLayerEnum.NONE;
    portalArea.CollisionMask = (uint)CollisionLayerEnum.PLAYER;
    portalArea.SetDeferred(Area3D.PropertyName.Monitorable, false);

    portalArea.BodyEntered += OnBodyEntered;

    destination ??= GetNode<Marker3D>("../PortalPosition");
  }

  private void OnBodyEntered(Node3D body) {
    body.GlobalPosition =
      destination.GlobalPosition +
      destination.GlobalBasis *
      new Vector3(0.0f, 0.0f, 4.0f);
    body.GlobalRotation = destination.GlobalRotation;
    (body as Player).GetComponent<VelocityComponent>().Stop();
  }
}

