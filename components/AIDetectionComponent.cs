using Godot;

[GlobalClass]
public partial class AIDetectionComponent : Node3D {
  private Enemy owner;
  private Player? player;

  private Area3D vision;
  private Area3D hearing;

  [Export] private int soundThreshold = 5;

  private bool playerInHearing;

  [Signal] public delegate void PlayerEnteredVisionEventHandler();
  [Signal] public delegate void PlayerExitedVisionEventHandler();

  [Signal] public delegate void PlayerEnteredHearingEventHandler();
  [Signal] public delegate void PlayerExitedHearingEventHandler();

  [Signal] public delegate void SoundHeardEventHandler(Vector3 soundPosition);

  public override void _Ready() {
    owner = GetParent<Enemy>();

    vision = GetNode<Area3D>("Vision");
    hearing = GetNode<Area3D>("Hearing");

    vision.SetDeferred(Area3D.PropertyName.Monitorable, false);
    hearing.SetDeferred(Area3D.PropertyName.Monitorable, false);

    vision.CollisionLayer = 0;
    hearing.CollisionLayer = 0;

    vision.CollisionMask = (uint)CollisionLayerEnum.PLAYER;
    hearing.CollisionMask = (uint)CollisionLayerEnum.PLAYER;

    vision.BodyEntered += body => EmitSignalPlayerEnteredVision();
    vision.BodyExited += body => EmitSignalPlayerExitedVision();

    hearing.BodyEntered += body => EmitSignalPlayerEnteredHearing();
    hearing.BodyExited += body => EmitSignalPlayerExitedHearing();

    hearing.BodyEntered += OnBodyEntered;
    hearing.BodyExited += OnBodyExited;
  }

  public override void _PhysicsProcess(double delta) {
    if(!playerInHearing || player == null) { return; }

    if(player.soundLevel >= soundThreshold) {
      EmitSignalSoundHeard(player.GlobalPosition);
    }
  }

  private void OnBodyEntered(Node3D body) {
    playerInHearing = true;
    player = body as Player;
  }

  private void OnBodyExited(Node3D body) {
    player = null;
    playerInHearing = false;
  }
}

