using Godot;

public partial class MuzzleFlashEffect : Node3D {
  private Sprite3D sprite;
  private float lifetime = 0.07f;
  private float timer;

  public override void _Ready() {
    sprite = new Sprite3D();
    
    // Create a simple white-hot texture procedurally
    Image image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
    for(int x = 0; x < 32; x++) {
      for(int y = 0; y < 32; y++) {
        float dx = (x - 16) / 16.0f;
        float dy = (y - 16) / 16.0f;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float alpha = Mathf.Clamp(1.0f - dist, 0.0f, 1.0f);
        alpha = Mathf.Pow(alpha, 0.5f);
        
        Color color;
        if(dist < 0.3f) {
          color = new Color(1.0f, 0.95f, 0.9f, alpha); // white core
        } else if(dist < 0.6f) {
          color = new Color(1.0f, 0.5f, 0.2f, alpha * 0.9f); // orange
        } else {
          color = new Color(0.69f, 0.0f, 0.12f, alpha * 0.7f); // crimson edge
        }
        image.SetPixel(x, y, color);
      }
    }
    
    ImageTexture texture = ImageTexture.CreateFromImage(image);
    sprite.Texture = texture;
    sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
    sprite.NoDepthTest = true;
    sprite.MaterialOverlay = new StandardMaterial3D();
    ((StandardMaterial3D)sprite.MaterialOverlay).BlendMode = BaseMaterial3D.BlendModeEnum.Add;
    ((StandardMaterial3D)sprite.MaterialOverlay).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
    
    sprite.Scale = new Vector3(0.3f, 0.3f, 0.3f);
    sprite.Position = new Vector3(0, 0, -0.01f);
    
    AddChild(sprite);
    
    timer = lifetime;
  }

  public override void _Process(double delta) {
    timer -= (float)delta;
    float t = Mathf.Clamp(timer / lifetime, 0.0f, 1.0f);
    sprite.Scale = new Vector3(0.3f * (1.0f + t * 0.5f), 0.3f * (1.0f + t * 0.5f), 0.3f);
    sprite.Modulate = new Color(1, 1, 1, t * t);
    
    if(timer <= 0.0f) {
      QueueFree();
    }
  }
}