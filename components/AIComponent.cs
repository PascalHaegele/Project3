using Godot;

public enum EnemyType { Melee, Ranged, Brute }
public enum AIState { Idle, Patrol, Chase, Attack, Search, Dead }

/// <summary>
/// AI component for enemy actors. Manages states, detection, and basic combat logic.
/// Attach as a child Node of an Actor (CharacterBody3D).
/// Uses the actor's VelocityComponent for movement and HealthComponent for death detection.
/// </summary>
[GlobalClass]
public partial class AIComponent : Node {
  [Export] public EnemyType enemyType = EnemyType.Melee;
  [Export] public float detectionRange = 12f;
  [Export] public float attackRange = 2f;
  [Export] public float searchDuration = 3f;

  [Export] public NavigationAgent3D navigationAgent;

  public AIState currentState = AIState.Patrol;

  private Actor actor;
  private HealthComponent healthComponent;
  private Player player;
  private float searchTimer;

  private RandomNumberGenerator rng = new();

  private InputPackage input = new();

  public InputPackage GetInput => input;

  [Signal] public delegate void IdlingEventHandler();
  [Signal] public delegate void PatrolingEventHandler();
  [Signal] public delegate void ChasingEventHandler();
  [Signal] public delegate void AttackingEventHandler();
  [Signal] public delegate void SearchingEventHandler();

  public override void _Ready() {
    actor = GetParent<Actor>();

    player = GetTree().Root.FindChild("Player", true, false) as Player;
    if(player == null) {
      GD.PrintErr($"{actor.Name}: Player not found in scene tree!");
    }

    navigationAgent ??= GetNode<NavigationAgent3D>("../NavigationAgent3D");

    if(navigationAgent == null) {
      GD.PrintErr(
        $"{actor.Name}: No NavigationAgent3D found! AI needs one for movement."
      );
    }

    navigationAgent.TargetPosition =
      actor.GlobalPosition +
      new Vector3(
        rng.Randf() * 4.0f + 1.0f,
        0.0f,
        rng.Randf() * 4.0f + 1.0f
      );

    healthComponent = actor.GetComponent<HealthComponent>();
    healthComponent.Died += OnDied;

    Idling += OnIdling;
    Patroling += OnPatroling;
    Chasing += OnChasing;
    Attacking += OnAttacking;
    Searching += OnSearching;

    rng.Randomize();
  }

  public override void _Process(double delta) {
  }

  public override void _PhysicsProcess(double delta) {
    if(player == null || currentState == AIState.Dead) { return; }

    input = new();

    float distance = actor.GlobalPosition.DistanceTo(player.GlobalPosition);

    switch(currentState) {
      case AIState.Idle: UpdateIdle(distance); break;
      case AIState.Patrol: UpdatePatrol(distance); break;
      case AIState.Chase: UpdateChase(distance, delta); break;
      case AIState.Attack: UpdateAttack(distance); break;
      case AIState.Search: UpdateSearch(distance, delta); break;
      case AIState.Dead: // handled above
      default: break;
    }
  }

  private void UpdateIdle(float distance) {
    if(distance <= detectionRange) {
      GD.Print($"{actor.Name}: Detected player - chasing!");
      currentState = AIState.Chase;
      EmitSignalChasing();
    }
  }

  private void UpdatePatrol(float distance) {
    if(distance <= detectionRange) {
      currentState = AIState.Chase;
      EmitSignalChasing();
    }

    // if(NavigationAgent.IsNavigationFinished()) { return; }

    if(navigationAgent.IsTargetReached()) {
      navigationAgent.TargetPosition =
        actor.GlobalPosition +
        new Vector3(
          rng.Randf() * 4.0f + 1.0f,
          0.0f,
          rng.Randf() * 4.0f + 1.0f
        );
    }

    Vector3 actorPosition = actor.GlobalTransform.Origin;
    Vector3 nextPathPosition = navigationAgent.GetNextPathPosition();

    Vector3 direction =
      actorPosition.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;
    input.direction = new(direction.X, direction.Z);

    if(!actorPosition.IsEqualApprox(actorPosition + actor.Direction)) {
      actor.LookAt(actorPosition - actor.Direction);
    }
  }

  private void UpdateChase(float distance, double delta) {
    // Face the player (Y-axis rotation only)
    // Move toward player using NavigationAgent or direct movement
    if(navigationAgent != null && !navigationAgent.IsNavigationFinished()) {
      navigationAgent.TargetPosition = player.GlobalPosition;
    }

    Vector3 actorPosition = actor.GlobalTransform.Origin;
    Vector3 nextPathPosition = navigationAgent.GetNextPathPosition();

    Vector3 direction =
      actorPosition.DirectionTo(nextPathPosition).Normalized();
    direction.Y = 0.0f;
    input.direction = new(direction.X, direction.Z);
    input.sprint = true;

    if(!actorPosition.IsEqualApprox(actorPosition + actor.Direction)) {
      actor.LookAt(actorPosition - actor.Direction);
    }

    // State transitions
    if(distance <= attackRange) {
      GD.Print($"{actor.Name}: Entering attack range!");
      currentState = AIState.Attack;
      EmitSignalAttacking();
    } else if(distance > detectionRange * 1.5f) {
      GD.Print($"{actor.Name}: Lost sight of player - searching!");
      currentState = AIState.Search;
      EmitSignalSearching();
      searchTimer = 0f;
    }
  }

  private void UpdateAttack(float distance) {
    // Attack logic can be extended later (e.g. emit signal, deal damage via HitBoxComponent)
    // For now, just return to chase if player leaves attack range
    if(distance > attackRange) {
      currentState = AIState.Chase;
      EmitSignalChasing();
    }
  }

  private void UpdateSearch(float distance, double delta) {
    searchTimer += (float)delta;
    if(searchTimer >= searchDuration) {
      searchTimer = 0f;
      GD.Print($"{actor.Name}: Search over - returning to patrol.");
      currentState = AIState.Patrol;
      EmitSignalPatroling();
    }
    if(distance <= detectionRange) {
      currentState = AIState.Chase;
      EmitSignalChasing();
    }
  }

  private void OnIdling() { }

  private void OnPatroling() { }

  private void OnChasing() { input.sprint = true; }

  private void OnAttacking() { }

  private void OnSearching() { }

  private void OnDied() {
    GD.Print($"{actor.Name}: AI died.");
    currentState = AIState.Dead;
  }

  /// <summary>
  /// Public method to force death (called externally, e.g. from a button or script).
  /// </summary>
  public void Die() {
    currentState = AIState.Dead;
  }
}
