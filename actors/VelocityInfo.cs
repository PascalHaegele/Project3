using Godot;

[GlobalClass]
public partial class VelocityInfo : Resource {
  [Export(PropertyHint.Range, "0.0f, 100.0f, 0.1f")]
  public float maxSpeed = 10.0f;

  [Export(PropertyHint.Range, "0.0f, 1.0f, 0.1f")]
  public float accelerationCoefficient = 1.0f;

  [Export(PropertyHint.Range, "0.0f, 1.0f, 0.1f")]
  public float decelerationCoefficient = 1.0f;

  private float speed;
  public float Speed {
    get => speed;
    set => speed = Mathf.Clamp(value, 0.0f, maxSpeed);
  }
}

