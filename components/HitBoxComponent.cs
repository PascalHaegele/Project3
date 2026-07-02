using Godot;

[GlobalClass]
public partial class HitBoxComponent : Area3D
{
    [Export] public float Damage { get; set; } = 10;

}
