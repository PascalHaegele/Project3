using Godot;
using System.Linq;
using System.Diagnostics;

[GlobalClass]
public partial class AIStateMachine : Node {
  public InputPackage GetInput => currentState.input;

  [Export] private AIState[] states;
  [Export] private AIState? startingState;
  private AIState currentState;

  [Export] private NavigationAgent3D navAgent;

  private Enemy actor;

  private Player player;
  private float playerDistance;

  private Vector3 leashPoint;
  private float leashDistance;

  public override void _Ready() {
    actor = GetParent<Enemy>();
    player = GetTree().Root.FindChild("Player", true, false) as Player;

    navAgent ??= GetNode<NavigationAgent3D>("../NavigationAgent3D");

    if(actor.patrolPath != null) {
      if(actor.patrolPath.Length > 0) {
        navAgent.TargetPosition = actor.patrolPath[0].GlobalPosition;
      }
    }

    leashPoint =
      actor.leashPoint == null ?
      actor.GlobalPosition :
      actor.leashPoint.GlobalPosition;

    foreach(AIState state in states) {
      state.Init(actor, player, this, navAgent);
      state.Transition += OnStateTransition;
    }

    currentState = startingState ?? states[0];

    if(currentState == null) {
      GD.PrintErr($"{actor.Name} StateMachine has no starting State");
    }

    currentState.Enter();
  }

  public override void _Process(double delta) {
    playerDistance = actor.GlobalPosition.DistanceTo(player.GlobalPosition);
    leashDistance = actor.GlobalPosition.DistanceTo(leashPoint);

    currentState.Update(delta);
    currentState.CheckRelevance(playerDistance, leashDistance);

    Debug
      .panel
      .AddProperty("Current State", currentState.GetType().ToString(), 1);
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

  private void OnStateTransition(AIState newState) {
    if(!states.Contains(newState)) {
      GD.PrintErr(
        $"{actor.Name} StateMachine does not contain {newState.GetType()}"
      );
      return;
    }

    if(newState == currentState) { return; }

    currentState.Exit();
    currentState = newState;
    newState.Enter();
  }
}

