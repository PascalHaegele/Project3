using Godot;

[GlobalClass]
public partial class VelocityInfo : Resource {
  [Export(PropertyHint.Range, "0.0f, 100.0f, 0.1f")]
  public float maxSpeed = 20.0f;

  [Export(PropertyHint.Range, "0.0f, 50.0f, 0.1f")]
  public float walkSpeed = 6.0f;

  [Export(PropertyHint.Range, "0.0f, 50.0f, 0.1f")]
  public float sprintSpeed = 10.0f;

  [Export(PropertyHint.Range, "0.0f, 1.0f, 0.1f")]
  public float accelerationCoefficient = 1.0f;

  [Export(PropertyHint.Range, "0.0f, 1.0f, 0.1f")]
  public float decelerationCoefficient = 1.0f;

  [Export(PropertyHint.Range, "0.0f, 20.0f, 0.1f")]
  public float jumpVelocity = 5.0f;

  [Export(PropertyHint.Range, "0.0f, 50.0f, 0.1f")]
  public float airborneSpeed = 4.0f;

  [Export(PropertyHint.Range, "0.0f, 15.0f, 0.1f")]
  public float dashDistance = 8.0f;

  [Export(PropertyHint.Range, "0.0f, 1.0f, 0.1f")]
  public float dashDuration = 0.2f;

  [Export(PropertyHint.Range, "0.0f, 10.0f, 0.1f")]
  public float dashCooldown = 0.8f;

  private float speed;
  public float Speed {
    get => speed;
    set => speed = Mathf.Clamp(value, 0.0f, maxSpeed);
  }
}

