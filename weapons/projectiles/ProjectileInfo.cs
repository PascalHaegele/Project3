using Godot;

[GlobalClass]
public partial class ProjectileInfo : Resource {
  [Export] public float speed = 50.0f;
  [Export] public float damage = 20.0f;
  [Export] public float range = 50.0f;
}

