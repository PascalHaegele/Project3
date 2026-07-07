using Godot;

[GlobalClass]
public partial class DebugDraw : MeshInstance3D {
  public override void _Ready() {
    Debug.draw = this;

    Visible = false;
    CastShadow = ShadowCastingSetting.Off;
    StandardMaterial3D mat = new();
    mat.VertexColorUseAsAlbedo = true;
    MaterialOverride = mat;
  }

  public override void _PhysicsProcess(double delta) {
    if(Mesh is ImmediateMesh mesh) { mesh.ClearSurfaces(); }
  }

  public override void _Input(InputEvent @event) {
    if(@event.IsActionPressed("debug_panel")) { Visible = !Visible; }
  }

  public void DrawLine(Vector3 a, Vector3 b, Color color) {
    if(!Visible) { return; }
    if(a.IsEqualApprox(b)) { return; }

    if(Mesh is ImmediateMesh mesh) {
      mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

      mesh.SurfaceSetColor(color);

      mesh.SurfaceAddVertex(a);
      mesh.SurfaceAddVertex(b);

      mesh.SurfaceEnd();
    }
  }

  public void DrawLineRelative(Vector3 a, Vector3 b, Color color) {
    DrawLine(a, a + b, color);
  }

  public void DrawLineThick(
    Vector3 a, Vector3 b,
    float thickness,
    bool pointy,
    Color color
  ) {
    if(!Visible) { return; }

    if(a.IsEqualApprox(b)) { return; }

    if(Mesh is ImmediateMesh mesh) {
      mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);

      mesh.SurfaceSetColor(color);

      float scaleFactor = 100.0f;
      Vector3 direction = a.DirectionTo(b);

      Vector3 normal =
        Mathf.Abs(direction.X) + Mathf.Abs(direction.Y) > Mathf.Epsilon ?
        new Vector3(-direction.Y, direction.X, 0.0f).Normalized() :
        new Vector3(0.0f, -direction.Z, direction.Y).Normalized();
      normal *= thickness / scaleFactor;

      byte[] indices = [4, 5, 0, 1, 2, 5, 6, 4, 7, 0, 3, 2, 7, 6];
      Vector3 localB = b - a;

      for(int i = 0; i < 14; i++) {
        Vector3 vertex =
          indices[i] < 4 ? normal :
          pointy ? normal / 3.0f + localB :
          normal + localB;

        Vector3 finalVertex =
          vertex.Rotated(
            direction,
            Mathf.Pi * (0.5f * (indices[i] % 4) + 0.25f)
          );
        finalVertex += a;
        mesh.SurfaceAddVertex(finalVertex);
      }

      mesh.SurfaceEnd();
    }
  }

  public void DrawLineRelativeThick(
    Vector3 a, Vector3 b,
    float thickness,
    bool pointy,
    Color color
  ) {
    if(!Visible) { return; }

    DrawLineThick(a, a + b, thickness, pointy, color);
  }
}

