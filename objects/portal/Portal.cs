using Godot;

public partial class Portal : StaticBody3D {
  [Export] public Marker3D destination;

  [Export] public bool isLoop;
  [Export] public bool isLevelChange;

  [Export(PropertyHint.File, "*.tscn")] private string? newLevelPath;

  private Area3D portalArea;

  [Signal]
  public delegate void ChangeLevelEventHandler(StringName newLevelPath);

  [Signal] public delegate void LoopEventHandler();

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
    material.ResourceLocalToScene = true;

    if(isLoop) {
      isLevelChange = true;
      newLevelPath ??= "res://maps/CS_Map.tscn";

      material.SetShaderParameter("color", new Vector3(0.8f, 0.1f, 0.7f));
    } else if(isLevelChange) {
      material.SetShaderParameter("color", new Vector3(0.2f, 1.0f, 0.2f));
    } else {
      destination ??= GetNode<Marker3D>("../PortalPosition");
      material.SetShaderParameter("color", new Vector3(1.0f, 1.0f, 0.5f));
    }
  }

  private void OnBodyEntered(Node3D body) {
    if(isLevelChange && newLevelPath != null) {
      if(isLoop) { EmitSignalLoop(); }
      EmitSignalChangeLevel(newLevelPath);
      return;
    }

    // body.GlobalPosition =
    //   destination.GlobalPosition +
    //   destination.GlobalBasis *
    //   new Vector3(0.0f, 0.0f, 4.0f);
    body.GlobalPosition = destination.GlobalPosition;
    body.GlobalRotation = destination.GlobalRotation;
    (body as Actor).GetComponent<VelocityComponent>().Stop();
  }
}

