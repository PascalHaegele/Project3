using Godot;
using System.Linq;
using System.Diagnostics;

[GlobalClass]
public partial class AIStateMachine : Node {
  [Export] private AIState[] states;
  [Export] private AIState? startingState;
  private AIState currentState;

  [Export] private NavigationAgent3D navAgent;

  private Enemy actor;

  private Player player;
  private float playerDistance;

  private Vector3 leashPoint;
  private float leashDistance;

  public InputPackage GetInput => currentState.input;

  public override void _Ready() {
    actor = GetParent<Enemy>();
    player = GetTree().Root.FindChild("Player", true, false) as Player;

    navAgent ??= GetNode<NavigationAgent3D>("../NavigationAgent3D");

    if(actor.enemyInfo.patrolPath != null) {
      if(actor.enemyInfo.patrolPath.Length > 0) {
        navAgent.TargetPosition = actor.enemyInfo.patrolPath[0];
      }
    }

    leashPoint =
      actor.enemyInfo.leashPoint == Vector3.Zero ?
      actor.GlobalPosition :
      actor.enemyInfo.leashPoint;

    foreach(AIState state in states) {
      state.ResourceLocalToScene = true;
      state.Init(actor, player, this, navAgent);
      state.Transition += OnStateTransition;
      state.Start();
    }

    currentState =
      GetStateOrNull<AIStatePatrol> == null || actor.enemyInfo.patrolPath == null ?
      GetState<AIStateIdle>() :
      GetState<AIStatePatrol>();

    if(currentState == null) {
      GD.PrintErr($"{actor.Name} AIStateMachine has no starting State");
    }

    currentState.Enter();
  }

  public override void _Process(double delta) {
    playerDistance = actor.GlobalPosition.DistanceTo(player.GlobalPosition);
    leashDistance = actor.GlobalPosition.DistanceTo(leashPoint);

    currentState.Update(delta);
    currentState.CheckRelevance(playerDistance, leashDistance);
  }

  public override void _PhysicsProcess(double delta) {
    currentState.PhysicsUpdate(delta);
  }

  public T? GetState<T>() where T : AIState {
    foreach(AIState state in states) { if(state is T) { return state as T; } }

    StackFrame frame = new StackTrace().GetFrame(1);
    GD.PrintErr(
      $"{actor.Name} AIStateMachine does not have requested AIState " +
      $"{typeof(T)} | " +
      $"Requested from {frame.GetMethod().DeclaringType.Name} " +
      $"in method {frame.GetMethod().Name}"
    );
    return null;
  }

  public T? GetStateOrNull<T>() where T : AIState {
    foreach(AIState state in states) { if(state is T) { return state as T; } }
    return null;
  }

  public void OnStateTransition(AIState? newState) {
    if(newState == null) { return; }
    if(!states.Contains(newState)) {
      GD.PrintErr(
        $"{actor.Name} AIStateMachine does not contain {newState.GetType()}"
      );
      return;
    }

    if(newState == currentState) { return; }

    currentState.Exit();
    currentState = newState;
    currentState.Enter();

    GD.Print($"{actor.Name} transitioned to {currentState.GetType()}");
  }
}

