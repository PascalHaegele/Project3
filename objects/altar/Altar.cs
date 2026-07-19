using Godot;
using System.Collections.Generic;

public partial class Altar : StaticBody3D, IInteractable {
  public enum AltarEffect {
    DamagePlayer,
    HealPlayer,
    IncreaseInsanity,
    DecreaseInsanity,
    SpawnEnemies,
  }

  private delegate void EffectFunction(Player player);

  private EffectFunction[] functions = [];

  [Export] private AltarEffect effect = AltarEffect.HealPlayer;

  [Export] private float damage = 20.0f;
  [Export] private float heal = 50.0f;
  [Export] private float insanityIncrease = 30.0f;
  [Export] private float insanityDecrease = 30.0f;

  [Export] private PackedScene enemy;
  [Export] private EnemyInfo enemyInfo;
  private readonly List<Marker3D> spawnPositions = new();

  public override void _Ready() {
    functions = [
      DamagePlayer,
      HealPlayer,
      IncreaseInsanity,
      DecreaseInsanity,
      SpawnEnemies,
    ];

    foreach(Node child in GetChildren()) {
      if(child is Marker3D marker) { spawnPositions.Add(marker); }
    }
  }

  public void Interact(Player player) {
    GD.Print($"Interaction with {Name}");
    EffectFunction f = functions[(int)effect];
    f(player);
  }

  private void DamagePlayer(Player player) {
    HealthComponent healthComponent = player.GetComponent<HealthComponent>();
    if(healthComponent.CurrentHealth - damage <= 0.0f) {
      damage = healthComponent.CurrentHealth - 1.0f;
    }
    healthComponent.TakeDamage(damage);
  }

  private void HealPlayer(Player player) {
    player.GetComponent<HealthComponent>().Heal(50.0f);
  }

  private void IncreaseInsanity(Player player) {
    player.GetComponent<InsanityComponent>().AddInsanity(30.0f);
  }

  private void DecreaseInsanity(Player player) {
    player.GetComponent<InsanityComponent>().AddInsanity(-30.0f);
  }

  private void SpawnEnemies(Player player) {
    for(int i = 0; i < spawnPositions.Count; i++) {
      Enemy e = enemy.Instantiate<Enemy>();
      e.enemyInfo = enemyInfo;
      e.enemyInfo.ResourceLocalToScene = true;
      AddChild(e);
      e.Position = spawnPositions[i].Position;
      e.Rotation = spawnPositions[i].Rotation;
    }
  }
}

