using Godot;

public partial class Portal : StaticBody3D {
  [Export] public Marker3D destination;

  [Export] public bool isLevelChange;
  [Export] private StringName? newLevelPath;

  private Area3D portalArea;

  [Signal]
  public delegate void ChangeLevelEventHandler(StringName newLevelPath);

  public override void _Ready() {
    CollisionLayer = (uint)CollisionLayerEnum.WORLD;
    CollisionMask = (uint)CollisionLayerEnum.NONE;

    portalArea = GetNode<Area3D>("PortalArea");
    portalArea.CollisionLayer = (uint)CollisionLayerEnum.NONE;
    portalArea.CollisionMask = (uint)CollisionLayerEnum.PLAYER;
    portalArea.SetDeferred(Area3D.PropertyName.Monitorable, false);

    portalArea.BodyEntered += OnBodyEntered;

    ShaderMaterial material =
      GetNode<MeshInstance3D>("MeshInstance3D")
        .MaterialOverride as ShaderMaterial;

    if(isLevelChange) {
      material.SetShaderParameter("color", new Vector3(0.2f, 1.0f, 0.2f));
    } else {
      destination ??= GetNode<Marker3D>("../PortalPosition");
      material.SetShaderParameter("color", new Vector3(1.0f, 1.0f, 0.5f));
    }
  }

  private void OnBodyEntered(Node3D body) {
    if(!isLevelChange) {
      body.GlobalPosition =
        destination.GlobalPosition +
        destination.GlobalBasis *
        new Vector3(0.0f, 0.0f, 4.0f);
      body.GlobalRotation = destination.GlobalRotation;
      (body as Actor).GetComponent<VelocityComponent>().Stop();
    } else {
      EmitSignalChangeLevel(newLevelPath);
    }
  }
}

