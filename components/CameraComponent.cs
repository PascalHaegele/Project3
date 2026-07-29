using Godot;

[GlobalClass]
public partial class CameraComponent : Camera3D {
  [Export(PropertyHint.Range, "0.1f, 1.0f, 0.01f")]
  private float sensitivity = 0.5f;

  [Export(PropertyHint.Range, "-90.0f, 0.0f, 0.1f, radians_as_degrees")]
  private float tiltLowerLimit = Mathf.DegToRad(-90.0f);

  [Export(PropertyHint.Range, "0.0f, 90.0f, 0.1f, radians_as_degrees")]
  private float tiltUpperLimit = Mathf.DegToRad(45.0f);

  private Node3D pivot;

  private float yawInput;
  private float pitchInput;

  public Vector3 Direction {
    get => pivot.Rotation;
    set => pivot.Rotation = value;
  }

  public Vector2 Motion { get; private set; }

  public float Sensitivity {
    get => sensitivity;
    set => sensitivity = Mathf.Clamp(value, 0.1f, 1.0f);
  }

  public override void _Ready() {
    pivot = GetParent<Node3D>();
  }

  public override void _PhysicsProcess(double delta) {
    Vector3 rotation = pivot.Rotation;
    rotation.X += pitchInput * (float)delta * sensitivity;
    rotation.X = Mathf.Clamp(rotation.X, tiltLowerLimit, tiltUpperLimit);

    rotation.Y += yawInput * (float)delta * sensitivity;
    rotation.Y = Mathf.PosMod(rotation.Y, Mathf.Tau);

    rotation.Z = 0.0f;

    pivot.Rotation = rotation;

    yawInput = 0.0f;
    pitchInput = 0.0f;
  }

  public override void _UnhandledInput(InputEvent @event) {
    bool mouseInput =
      @event is InputEventMouseMotion &&
      Input.MouseMode == Input.MouseModeEnum.Captured;

    if(mouseInput) {
      InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;

      Motion = mouseMotion.Relative;

      yawInput = -Motion.X;
      pitchInput = -Motion.Y;
    }
  }

  public void Shake(float amount, float duration) {
    if(amount <= 0.0f || duration <= 0.0f) { return; }

    float halfDuration = duration * 0.5f;
    Vector3 oldRotation = Rotation;
    Vector3 newRotation =
      Rotation +
      new Vector3(
        (float)GD.RandRange(-amount, amount),
        (float)GD.RandRange(-amount, amount),
        (float)GD.RandRange(-amount, amount)
      );

    Tween t = CreateTween();

    _ = t
      .TweenProperty(this, "rotation", newRotation, halfDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

    _ = t
      .TweenProperty(this, "rotation", oldRotation, halfDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
  }

  public void RecoilKick(float amount) {
    if(amount <= 0.0f) { return; }

    Vector3 oldPosition = Position;
    Vector3 newPosition = Position + new Vector3(0.0f, amount * 0.6f, -amount);

    Tween t = CreateTween();

    _ = t
      .TweenProperty(this, "position", newPosition, 0.04f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

    _ = t
      .TweenProperty(this, "position", oldPosition, 0.12f)
      .SetEase(Tween.EaseType.Out)
      .SetTrans(Tween.TransitionType.Sine);
  }
}

