using Godot;

[GlobalClass]
public partial class AIDetectionComponent : Node3D {
  private Enemy enemy;
  private Player player;

  private Area3D vision;
  private Area3D hearing;

  [Export] private int soundThreshold = 5;
  [Export(PropertyHint.Range, "0.0f, 90.0f, 1.0f, radians_as_degrees")]
  private float fov = Mathf.DegToRad(70.0f);
  private float cosHalfFov;

  private bool playerInHearing;
  private bool playerInVision;

  [Signal] public delegate void SoundHeardEventHandler(Vector3 position);
  [Signal] public delegate void SeeingPlayerEventHandler(Vector3 position);

  public override void _Ready() {
    enemy = GetParent<Enemy>();
    player = GetTree().Root.FindChild("Player", true, false) as Player;

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

    cosHalfFov = Mathf.Cos(fov / 2.0f);
  }

  public override void _PhysicsProcess(double delta) {
    if(playerInHearing) {
      enemy.aiInfo.soundPosition = player.GlobalPosition;
      if(player.soundLevel >= soundThreshold) {
        enemy.hearingPlayer = enemy.aiInfo.soundHeard = true;
        if(!player.GlobalPosition.IsEqualApprox(enemy.GlobalPosition)) {
          enemy.LookAt(player.GlobalPosition);
        }
      } else {
        enemy.hearingPlayer = false;
      }
    }

    if(playerInVision) {
      enemy.aiInfo.targetPosition = player.GlobalPosition;

      Vector3 direction =
        enemy.GlobalPosition.DirectionTo(player.GlobalPosition).Normalized();
      Vector3 dir = enemy.GlobalBasis.Transposed() * direction;
      float angle = Mathf.Atan2(dir.X, -dir.Z);

      enemy.playerInVision = enemy.aiInfo.hasTarget =
        angle <= cosHalfFov && angle >= -cosHalfFov;
    }
  }

  private void OnBodyEnteredVision(Node3D body) {
    GD.Print("Player entered vision");
    playerInVision = true;
  }

  private void OnBodyExitedVision(Node3D body) {
    playerInVision = false;
    enemy.playerInVision = enemy.aiInfo.hasTarget = false;
  }

  private void OnBodyEnteredHearing(Node3D body) {
    GD.Print("Player entered hearing");
    playerInHearing = true;
  }

  private void OnBodyExitedHearing(Node3D body) {
    playerInHearing = false;
    enemy.hearingPlayer = false;
  }
}

