using Godot;

[GlobalClass]
public partial class VelocityComponent : Node {
  private Vector3 velocity;
  private VelocityInfo info;
  private float dashBoost;

  public override void _Ready() {
    info = GetParent<Actor>().velocityInfo;
  }

  public void AccelerateToVelocity(Vector3 targetVelocity) {
    velocity.X =
      Mathf.Lerp(velocity.X, targetVelocity.X, info.decelerationCoefficient);
    velocity.Z =
      Mathf.Lerp(velocity.Z, targetVelocity.Z, info.decelerationCoefficient);
  }

  public void AccelerateInDirection(Vector3 direction) {
    AccelerateToVelocity(direction * info.Speed);
  }

  public void AddVelocityInDirection(Vector3 direction, float magnitude = 1.0f) {
    velocity += direction * magnitude;
  }

  public void SetYVelocity(float magnitude = 1.0f, int direction = 1) {
    direction = direction < 0 ? -1 : 1;
    velocity.Y = direction * magnitude;
  }

  public void SetDashBoost(float value) {
    dashBoost = value;
  }

  public void ClearDashBoost() {
    dashBoost = 0.0f;
  }

  public void Decelerate() {
    velocity.X = Mathf.Lerp(velocity.X, 0.0f, info.decelerationCoefficient);
    velocity.Z = Mathf.Lerp(velocity.Z, 0.0f, info.decelerationCoefficient);
  }

  public void Stop() {
    velocity.X = 0.0f;
    velocity.Z = 0.0f;
  }

  public void Move(CharacterBody3D body) {
    Vector3 movement = velocity;
    if(dashBoost > 0.0f) {
      movement += body.Transform.Basis.Z * dashBoost;
    }
    body.Velocity = movement;
    _ = body.MoveAndSlide();
  }

  public void Move(StaticBody3D body) {
    _ = body.MoveAndCollide(velocity);
  }
}

