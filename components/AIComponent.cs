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
  [Export] public EnemyType EnemyType = EnemyType.Melee;
  [Export] public float DetectionRange = 12f;
  [Export] public float AttackRange = 2f;
  [Export] public float SearchDuration = 3f;

  [Export] public NavigationAgent3D NavigationAgent;
  [Export] public HealthComponent HealthComponent;

  public AIState CurrentState = AIState.Idle;

  private CharacterBody3D _owner;
  private Node3D _player;
  private VelocityComponent _velocityComponent;
  private float _searchTimer;

  // Reference speed used when Chase is handled externally (e.g. by a state)
  public float MoveSpeed = 3f;

  public override void _Ready() {
    _owner = GetParent<CharacterBody3D>();

    // Resolve player reference via the actor's tree
    _player = GetTree().Root.FindChild("Player", true, false) as Node3D;
    if(_player == null) {
      GD.PrintErr($"{_owner.Name}: Player not found in scene tree!");
    }

    // Get VelocityComponent from the actor (component-based lookup)

    _velocityComponent =
      _owner is Actor actor ?
      actor.GetComponent<VelocityComponent>() :
      _owner.GetNodeOrNull<VelocityComponent>("VelocityComponent");

    if(_velocityComponent == null) {
      GD.PrintErr($"{_owner.Name}: No VelocityComponent found!");
    }

    // Automatically find NavigationAgent if not manually assigned

    NavigationAgent ??=
      _owner.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");

    if(NavigationAgent == null) {
      GD.PrintErr(
        $"{_owner.Name}: No NavigationAgent3D found! AI needs one for movement."
      );
    }

    // Subscribe to death event from HealthComponent

    if(HealthComponent != null) { HealthComponent.Died += OnDied; }
    else if(_owner is Actor actorWithHealth) {
      HealthComponent = actorWithHealth.GetComponent<HealthComponent>();
      if(HealthComponent != null) { HealthComponent.Died += OnDied; }
    }
  }

  public override void _PhysicsProcess(double delta) {
    if(_player == null || CurrentState == AIState.Dead) { return; }

    float distance = _owner.GlobalPosition.DistanceTo(_player.GlobalPosition);

    switch(CurrentState) {
      case AIState.Idle: UpdateIdle(distance); break;
      case AIState.Patrol: UpdatePatrol(distance); break;
      case AIState.Chase: UpdateChase(delta, distance); break;
      case AIState.Attack: UpdateAttack(distance); break;
      case AIState.Search: UpdateSearch(delta); break;
      case AIState.Dead: // handled above
      default: break;
    }
  }

  private void UpdateIdle(float distance) {
    if(distance <= DetectionRange) {
      GD.Print($"{_owner.Name}: Detected player - chasing!");
      CurrentState = AIState.Chase;
    }
  }

  private void UpdatePatrol(float distance) {
    if(distance <= DetectionRange) { CurrentState = AIState.Chase; }
  }

  private void UpdateChase(double delta, float distance) {
    // Face the player (Y-axis rotation only)
    Vector3 lookTarget =
      new(
        _player.GlobalPosition.X,
        _owner.GlobalPosition.Y,
        _player.GlobalPosition.Z
      );
    _owner.LookAt(lookTarget);

    // Move toward player using NavigationAgent or direct movement
    if(NavigationAgent != null && NavigationAgent.IsNavigationFinished()) {
      NavigationAgent.TargetPosition = _player.GlobalPosition;
    }

    // Use VelocityComponent for actual movement toward player
    if(_velocityComponent != null) {
      Vector3 direction =
        (_player.GlobalPosition - _owner.GlobalPosition).Normalized();
      direction.Y = 0;
      _velocityComponent.AccelerateInDirection(direction);
      _velocityComponent
        .AddVelocityInDirection(_owner.GetGravity() * (float)delta);
      _velocityComponent.Move(_owner);
    }

    // State transitions
    if(distance <= AttackRange) {
      GD.Print($"{_owner.Name}: Entering attack range!");
      CurrentState = AIState.Attack;
    } else if(distance > DetectionRange * 1.5f) {
      GD.Print($"{_owner.Name}: Lost sight of player - searching!");
      CurrentState = AIState.Search;
      _searchTimer = 0f;
    }
  }

  private void UpdateAttack(float distance) {
    // Attack logic can be extended later (e.g. emit signal, deal damage via HitBoxComponent)
    // For now, just return to chase if player leaves attack range
    if(distance > AttackRange) {
      CurrentState = AIState.Chase;
    }
  }

  private void UpdateSearch(double delta) {
    _searchTimer += (float)delta;
    if(_searchTimer >= SearchDuration) {
      _searchTimer = 0f;
      GD.Print($"{_owner.Name}: Search over - returning to idle.");
      CurrentState = AIState.Idle;
    }
  }

  private void OnDied() {
    GD.Print($"{_owner.Name}: AI died.");
    CurrentState = AIState.Dead;
  }

  /// <summary>
  /// Public method to force death (called externally, e.g. from a button or script).
  /// </summary>
  public void Die() {
    CurrentState = AIState.Dead;
  }
}
