using Godot;

[GlobalClass]
public abstract partial class Enemy : Actor {
  [Export] public EnemyInfo info;

  public AnimationPlayer animationPlayer;

  public bool playerInVision;
  public bool hearingPlayer;

  protected AIStateMachine aiStateMachine;
  protected StateMachine stateMachine;
  protected VelocityComponent velocityComponent;
  protected HealthComponent healthComponent;
  protected AIDetectionComponent detectionComponent;

  public override void _Ready() {

    info.ResourceLocalToScene = true;

    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

    aiStateMachine = GetComponent<AIStateMachine>();
    stateMachine = GetComponent<StateMachine>();
    velocityComponent = GetComponent<VelocityComponent>();
    healthComponent = GetComponent<HealthComponent>();
    detectionComponent = GetComponent<AIDetectionComponent>();

    detectionComponent.PlayerEnteredVision += OnPlayerEnteredVision;
    detectionComponent.PlayerExitedVision += OnPlayerExitedVision;
    // detectionComponent.PlayerEnteredHearing += OnPlayerEnteredHearing;
    detectionComponent.PlayerExitedHearing += OnPlayerExitedHearing;
  }

  protected virtual void OnPlayerEnteredVision() {
    playerInVision = true;
    GD.Print($"Player entered Vision of {Name}");
  }

  protected virtual void OnPlayerExitedVision() {
    playerInVision = false;
    GD.Print($"Player exited Vision of {Name}");
  }

  // protected virtual void OnPlayerEnteredHearing() {
  //   playerInHearing = true;
  //   GD.Print($"Player entered Hearing of {Name}");
  // }

  protected virtual void OnPlayerExitedHearing() {
    hearingPlayer = false;
    GD.Print($"Player exited Hearing of {Name}");
  }
}

