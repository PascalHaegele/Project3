using Godot;

[GlobalClass]
public partial class AIDetectionComponent : Node3D {
  private Enemy enemy;
  private Player? player;

  private Area3D vision;
  private Area3D hearing;

  [Export] private int soundThreshold = 5;

  private bool playerInHearing;
  private bool playerInVision;

  [Signal] public delegate void SoundHeardEventHandler(Vector3 position);
  [Signal] public delegate void SeeingPlayerEventHandler(Vector3 position);

  public override void _Ready() {
    enemy = GetParent<Enemy>();

    vision = GetNode<Area3D>("Vision");
    hearing = GetNode<Area3D>("Hearing");

    vision.SetDeferred(Area3D.PropertyName.Monitorable, false);
    hearing.SetDeferred(Area3D.PropertyName.Monitorable, false);

    vision.CollisionLayer = (uint)CollisionLayerEnum.NONE;
    hearing.CollisionLayer = (uint)CollisionLayerEnum.NONE;

    vision.CollisionMask = (uint)CollisionLayerEnum.PLAYER;
    hearing.CollisionMask = (uint)CollisionLayerEnum.PLAYER;

    vision.BodyEntered += OnBodyEnteredVision;
    vision.BodyExited += OnBodyExitedVision;

    hearing.BodyEntered += OnBodyEnteredHearing;
    hearing.BodyExited += OnBodyExitedHearing;
  }

  public override void _PhysicsProcess(double delta) {
    if(playerInHearing && player != null) {
      if(player.soundLevel >= soundThreshold) {
        enemy.hearingPlayer = true;
        if(!player.GlobalPosition.IsEqualApprox(enemy.GlobalPosition)) {
          LookAt(player.GlobalPosition);
        }
        GD.Print($"{enemy.Name} hearing sound");
        EmitSignalSoundHeard(player.GlobalPosition);
      }
    }

    if(playerInVision && player != null) {
      float angle = (-enemy.GlobalBasis.Z).AngleTo(player.GlobalPosition);
      if(angle < Mathf.DegToRad(60.0f)) {
        enemy.playerInVision = true;
        GD.Print($"{enemy.Name} seeing player");
        EmitSignalSeeingPlayer(player.GlobalPosition);
      }
    }
  }

  private void OnBodyEnteredVision(Node3D body) {
    player = body as Player;
    playerInVision = true;
  }

  private void OnBodyExitedVision(Node3D body) {
    playerInVision = false;
    enemy.playerInVision = false;
    player = null;
  }

  private void OnBodyEnteredHearing(Node3D body) {
    player = body as Player;
    playerInHearing = true;
  }

  private void OnBodyExitedHearing(Node3D body) {
    playerInHearing = false;
    enemy.hearingPlayer = false;
    player = null;
  }
}

