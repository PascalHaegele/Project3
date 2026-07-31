using Godot;
using FmodSharp;

public partial class CleaverTrap : Node3D, ITrap {
  [Export] private float damage = 15.0f;

  [Export] private HitboxComponent hitbox;
  private AnimationPlayer animationPlayer;

  public override void _Ready() {
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    hitbox.damage = damage;
    hitbox.DisableCollisionShapes();
  }

  public void Activate() {
    if(!animationPlayer.IsPlaying()) {
      hitbox.EnableCollisionShapes();
      animationPlayer.Play("activate");
      FmodServerWrapper.PlayOneShotAttached("event:/Trap_Sound_Action", this);
    }
  }
}

