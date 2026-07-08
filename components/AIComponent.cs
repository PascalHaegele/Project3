// using Godot;
//
// public enum EnemyType { Melee, Ranged, Brute }
// public enum AIStateEnum { Idle, Patrol, Chase, Attack, Search, Dead }
//
// /// <summary>
// /// AI component for enemy actors.
// /// Manages states, detection, and basic combat logic.
// /// Attach as a child Node of an Actor (CharacterBody3D).
// /// Uses the actor's VelocityComponent for movement
// /// and HealthComponent for death detection.
// /// </summary>
// [GlobalClass]
// public partial class AIComponent : Node {
//   [Export] public EnemyType enemyType = EnemyType.Melee;
//   [Export] public float detectionRange = 12f;
//   [Export] public float attackRange = 2f;
//   [Export] public float searchDuration = 3f;
//   [Export] public float leashLength = 10.0f;
//
//   [Export] public NavigationAgent3D navAgent;
//   [Export] public AIStateEnum currentState = AIStateEnum.Idle;
//
//   public float searchTimer;
//
//   private int patrolIndex;
//
//   private Enemy actor;
//   private HealthComponent healthComponent;
//
//   private Player player;
//   private float playerDistance;
//
//   private Vector3 leashPoint;
//   private float leashDistance;
//
//   private InputPackage input = new();
//
//   public InputPackage GetInput() => input;
//
//   [Signal] public delegate void AttackingEventHandler();
//
//   public override void _Ready() {
//     actor = GetParent<Enemy>();
//     leashPoint =
//       actor.leashPoint == null ?
//       actor.GlobalPosition :
//       actor.leashPoint.GlobalPosition;
//
//     player = GetTree().Root.FindChild("Player", true, false) as Player;
//     if(player == null) {
//       GD.PrintErr($"{actor.Name}: Player not found in scene tree!");
//     }
//
//     navAgent ??= GetNode<NavigationAgent3D>("../NavigationAgent3D");
//
//     if(navAgent == null) {
//       GD.PrintErr(
//         $"{actor.Name}: No NavigationAgent3D found! AI needs one for movement."
//       );
//     }
//
//     if(actor.patrolPath != null) {
//       navAgent.TargetPosition = actor.patrolPath[0].GlobalPosition;
//     }
//
//     healthComponent = actor.GetComponent<HealthComponent>();
//     healthComponent.Died += OnDied;
//   }
//
//   public override void _PhysicsProcess(double delta) {
//     if(player == null || currentState == AIStateEnum.Dead) { return; }
//
//     input = new();
//
//     playerDistance = actor.GlobalPosition.DistanceTo(player.GlobalPosition);
//     leashDistance = actor.GlobalPosition.DistanceTo(leashPoint);
//
//     switch(currentState) {
//       case AIStateEnum.Idle: UpdateIdle(); break;
//       case AIStateEnum.Patrol: UpdatePatrol(); break;
//       case AIStateEnum.Chase: UpdateChase(); break;
//       case AIStateEnum.Attack: UpdateAttack(); break;
//       case AIStateEnum.Search: UpdateSearch(delta); break;
//       case AIStateEnum.Dead: // handled above
//       default: break;
//     }
//   }
//
//   private void UpdateIdle() {
//     input.direction = Vector2.Zero;
//   }
//
//   private void UpdatePatrol() {
//     if(navAgent.IsTargetReached()) {
//       patrolIndex = Mathf.PosMod(++patrolIndex, actor.patrolPath.Length - 1);
//       navAgent.TargetPosition = actor.patrolPath[patrolIndex].GlobalPosition;
//     }
//
//     Vector3 actorPosition = actor.GlobalTransform.Origin;
//     Vector3 nextPathPosition = navAgent.GetNextPathPosition();
//
//     Vector3 direction =
//       actorPosition.DirectionTo(nextPathPosition).Normalized();
//     direction.Y = 0.0f;
//     input.direction = new(direction.X, direction.Z);
//
//     if(!actorPosition.IsEqualApprox(actorPosition + actor.Direction)) {
//       actor.LookAt(actorPosition + actor.Direction);
//     }
//   }
//
//   private void UpdateChase() {
//     // Face the player (Y-axis rotation only)
//     // Move toward player using NavigationAgent or direct movement
//     if(!navAgent.IsNavigationFinished()) {
//       navAgent.TargetPosition = player.GlobalPosition;
//     }
//
//     Vector3 actorPosition = actor.GlobalTransform.Origin;
//     Vector3 nextPathPosition = navAgent.GetNextPathPosition();
//
//     Vector3 direction =
//       actorPosition.DirectionTo(nextPathPosition).Normalized();
//     direction.Y = 0.0f;
//     input.direction = new(direction.X, direction.Z);
//     input.sprint = true;
//
//     if(!actorPosition.IsEqualApprox(actorPosition + actor.Direction)) {
//       actor.LookAt(actorPosition + actor.Direction);
//     }
//
//     // State transitions
//     if(playerDistance <= attackRange) {
//       GD.Print($"{actor.Name}: Entering attack range!");
//       EmitSignalAttacking();
//       currentState = AIStateEnum.Attack;
//     }
//   }
//
//   private void UpdateAttack() {
//     if(playerDistance > attackRange) { currentState = AIStateEnum.Chase; }
//   }
//
//   private void UpdateSearch(double delta) {
//     searchTimer += (float)delta;
//     if(searchTimer >= searchDuration) {
//       searchTimer = 0.0f;
//       GD.Print($"{actor.Name}: Search over - returning to patrol.");
//       currentState = AIStateEnum.Patrol;
//     }
//     if(leashDistance > leashLength) {
//       GD.Print($"{actor.Name} leash length reached");
//       searchTimer = 0.0f;
//       currentState = AIStateEnum.Patrol;
//     }
//   }
//
//   private void OnDied() {
//     GD.Print($"{actor.Name}: AI died.");
//     currentState = AIStateEnum.Dead;
//   }
//
//   /// <summary>
//   /// Public method to force death
//   /// (called externally, e.g. from a button or script).
//   /// </summary>
//   public void Die() {
//     currentState = AIStateEnum.Dead;
//   }
// }
