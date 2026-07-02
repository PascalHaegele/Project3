using Godot;

[GlobalClass]
public partial class HurtBoxComponent : Area3D
{
    // Wir exportieren die Komponente, damit du sie im Editor zuweisen kannst
    [Export] public HealthComponent ActorHealthComponent { get; set; }

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area3D area)
    {
        // 1. Prüfen, ob es eine Hitbox ist
        // 2. Prüfen, ob eine HealthComponent zugewiesen wurde
        if (area is HitBoxComponent hitbox && ActorHealthComponent != null)
        {
            // Direkte Übergabe des Schadens an deine Komponente
            ActorHealthComponent.TakeDamage(hitbox.Damage);
            GD.Print("took damage");
        }
    }
}
