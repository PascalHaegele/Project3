using Godot;

[GlobalClass]
public partial class CameraComponent : Camera3D {
  [Export(PropertyHint.Range, "0.0f, 1.0f, 0.1f")]
  private float sensitivity = 0.5f;

  [Export(PropertyHint.Range, "-90.0f, 0.0f, 0.1f, radians_as_degrees")]
  private float tiltLowerLimit = Mathf.DegToRad(-90.0f);

  [Export(PropertyHint.Range, "0.0f, 90.0f, 0.1f, radians_as_degrees")]
  private float tiltUpperLimit = Mathf.DegToRad(45.0f);

  [Export]
  private Node3D pivot;

  private float yawInput;
  private float pitchInput;

  public Vector3 Direction => pivot.Rotation;

  public override void _PhysicsProcess(double delta) {
    Vector3 rotation = pivot.Rotation;
    rotation.X += pitchInput * (float)delta * sensitivity;
    rotation.X = Mathf.Clamp(rotation.X, tiltLowerLimit, tiltUpperLimit);

    rotation.Y += yawInput * (float)delta * sensitivity;
    rotation.Y = (float)Mathf.Wrap(rotation.Y, 0.0, Mathf.Tau);

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
      InputEventMouseMotion mouseMotion = (InputEventMouseMotion)@event;

      yawInput = -mouseMotion.Relative.X;
      pitchInput = -mouseMotion.Relative.Y;
    }
  }
}

