using Godot;

public partial class Chest : StaticBody3D, IInteractable {
  private AnimationPlayer animationPlayer;
  private LootComponent loot;

  public override void _Ready() {
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    loot = GetNode<LootComponent>("LootComponent");
  }

  public void Interact(Player player) {
    animationPlayer.Play("open");
    GD.Print($"Interaction with {Name}");

    player.GetComponent<InventoryComponent>().AddItem(loot.GenerateLoot());
  }
}

