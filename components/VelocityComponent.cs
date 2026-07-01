using Godot;

[GlobalClass]
public partial class VelocityComponent : Node {
  public Vector3 velocity;

  public VelocityStats stats;

  public void AccelerateToVelocity(Vector3 targetVelocity) {
    velocity = velocity.Lerp(targetVelocity, stats.accelerationCoefficient);
  }

  public void AccelerateInDirection(Vector3 direction) {
    AccelerateToVelocity(direction * stats.Speed);
  }

  public void ApplyGravity(Vector3 gravity) {
    velocity += gravity;
  }

  public void Decelerate() {
    velocity = velocity.Lerp(Vector3.Zero, stats.decelerationCoefficient);
  }

  public void Move(CharacterBody3D body) {
    body.Velocity = velocity;
    _ = body.MoveAndSlide();
  }
}

