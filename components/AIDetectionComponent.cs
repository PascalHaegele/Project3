using Godot;

[GlobalClass]
public partial class AIDetectionComponent : Node3D {
  private Area3D vision;
  private Area3D hearing;

  [Signal] public delegate void PlayerEnteredVisionEventHandler();
  [Signal] public delegate void PlayerExitedVisionEventHandler();

  [Signal] public delegate void PlayerEnteredHearingEventHandler();
  [Signal] public delegate void PlayerExitedHearingEventHandler();

  public override void _Ready() {
    vision = GetNode<Area3D>("Vision");
    hearing = GetNode<Area3D>("Hearing");

    vision.Monitorable = false;
    hearing.Monitorable = false;

    vision.CollisionMask = (uint)CollisionLayerEnum.PLAYER;
    hearing.CollisionMask = (uint)CollisionLayerEnum.PLAYER;

    vision.BodyEntered += body => EmitSignalPlayerEnteredVision();
    vision.BodyExited += body => EmitSignalPlayerExitedVision();

    hearing.BodyEntered += body => EmitSignalPlayerEnteredHearing();
    hearing.BodyExited += body => EmitSignalPlayerExitedHearing();
  }
}

