using Godot;
using System.Collections.Generic;

public partial class TestEnemy : Enemy, IHitable {
  private BehaviorTree behaviorTree;
  private TestEnemyStateMachine stateMachine;
  private AIDetectionComponent detectionComponent;

  private HitboxComponent hitboxComponent;
  private ProgressBar healthBar;

  [Export] private Material dissolveMaterial;

  public override void _Ready() {
    base._Ready();

    behaviorTree = GetComponent<BehaviorTree>();
    stateMachine = GetComponent<TestEnemyStateMachine>();
    detectionComponent = GetComponent<AIDetectionComponent>();

    healthComponent.HealthChanged += OnHealthChanged;
    healthComponent.Died += OnDeath;

    hitboxComponent = GetComponent<HitboxComponent>();
    hitboxComponent.damage = 10.0f;
    hitboxComponent.CollisionLayer = (uint)CollisionLayerEnum.ENEMY_HITBOX;
    hitboxComponent.CollisionMask = (uint)CollisionLayerEnum.PLAYER_HURTBOX;
    hitboxComponent.DisableCollisionShapes();

    healthBar = GetComponent<ProgressBar>();
    healthBar.MaxValue = healthComponent.maxHealth;
    healthBar.Value = healthComponent.CurrentHealth;

    ApplyDifficulty();
    healthComponent.Reset();
  }

  public override void _PhysicsProcess(double delta) {
    if(!healthComponent.IsAlive) { return; }

    input = behaviorTree.GetInput;
    behaviorTree.UpdateInfo(aiInfo);
    stateMachine.UpdateInput(input);

    Vector3 direction = new(input.direction.X, 0.0f, input.direction.Y);
    Direction = direction;

    if(!IsOnFloor()) {
      velocityComponent.AddVelocityInDirection(GetGravity() * (float)delta);
    }
    velocityComponent.Move(this);
  }

  public void RecieveHit(HitInfo hitInfo) {
    healthComponent.TakeDamage(hitInfo.damage);

    Vector3 direction = hitInfo.direction;
    direction.Y = 0.0f;
    aiInfo.shotFromDirection = direction;
    aiInfo.beeingShot = true;

    // WICHTIG: EmitSignalKilled wurde hier entfernt! 
    // Es darf erst gefeuert werden, wenn die Animation fertig ist!
  }

  protected override void ApplyDifficulty() {
    velocityInfo.multiplier = difficultyInfo.speedMultiplier;
    hitboxComponent.damageMultiplier = difficultyInfo.damageMultiplier;
    healthComponent.multiplier = difficultyInfo.healthMultiplier;
  }

  private void OnHealthChanged(float newHealth) {
    healthBar.Value = healthComponent.CurrentHealth;
  }

  private async void OnDeath() {
    velocityComponent.Stop();
    hitboxComponent.DisableCollisionShapes();

    // Sicherheitscheck, ob das Material im Inspector zugewiesen wurde
    if (dissolveMaterial != null && dissolveMaterial is ShaderMaterial baseShader) {
      
      // WICHTIG: Wir duplizieren das Material, damit es für DIESEN speziellen Gegner einzigartig ist.
      ShaderMaterial uniqueShader = (ShaderMaterial)baseShader.Duplicate();
      uniqueShader.SetShaderParameter("t", 0.0);
      uniqueShader.SetShaderParameter("noise_scale", 1.0);

      List<MeshInstance3D> meshList = new List<MeshInstance3D>();
      FindMeshesRecursive(this, meshList);

      // Weist allen Körperteilen (inklusive Schulter und Schwert) das EINZIGARTIGE Material zu
      foreach (MeshInstance3D mesh in meshList) {
          mesh.MaterialOverride = uniqueShader;
      }

      Tween tween = CreateTween();
      tween.TweenMethod(
        Callable.From((float value) => uniqueShader.SetShaderParameter("t", value)),
        0.0, 1.0, 2.0
      );
      
      // Warte 2 Sekunden, bis der Effekt komplett fertig ist
      await ToSignal(tween, Tween.SignalName.Finished);
    }

    // Erst JETZT dem GameManager sagen, dass der Gegner tot ist, und ihn dann löschen!
    EmitSignalKilled(this);
    QueueFree();
  }

  // Rekursive Suche, die garantiert jeden Knochen und jedes Rüstungsteil findet
  private void FindMeshesRecursive(Node parent, List<MeshInstance3D> meshList) {
    foreach (Node child in parent.GetChildren()) {
      if (child is MeshInstance3D mi && mi.Mesh != null) {
        meshList.Add(mi);
      }
      FindMeshesRecursive(child, meshList);
    }
  }
}