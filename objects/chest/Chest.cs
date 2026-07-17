using Godot;

public partial class Chest : StaticBody3D, IInteractable {
  [Export] private PackedScene pickup;

  private AnimationPlayer animationPlayer;

  public override void _Ready() {
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
  }

  public void Interact(Player player) {
    animationPlayer.Play("open");
    GD.Print($"Interaction with {Name}");

    Pickup p = pickup.Instantiate<Pickup>();
    AddChild(p);
    p.Position = new(0.0f, 1.2f, 0.0f);
    p.ApplyImpulse(new(0.0f, 3.0f, 2.0f));
  }
}

