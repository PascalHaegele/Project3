using Godot;
using System.Collections.Generic;

public partial class EventManager : Node {
  private Player player;
  private InsanityComponent playerInsanityComponent;

  private Marker3D playerSpawn;

  private WorldEnvironment environment;
  private ShaderMaterial skyShader;

  private readonly List<PortalArea> portalAreas = new();
  private readonly List<Altar> altars = new();

  public override void _Ready() {
    player = GetTree().Root.FindChild("Player", true, false) as Player;
    playerInsanityComponent = player.GetComponent<InsanityComponent>();

    playerInsanityComponent.InsanityChanged += OnInsanityChanged;
    playerInsanityComponent.InsanityLevelChanged += OnInsanityLevelChanged;

    playerSpawn = GetNode<Marker3D>("PlayerSpawn");

    player.GetComponent<HealthComponent>().Died += OnPlayerDeath;

    environment =
      GetTree()
        .Root
        .FindChild("WorldEnvironment", true, false) as WorldEnvironment;

    skyShader = environment.Environment.Sky.SkyMaterial as ShaderMaterial;

    if(GetNodeOrNull("HitstopManager") == null) {
      AddChild(new HitstopManager());
    }

    foreach(Node child in GetChildren()) {
      if(child is PortalArea portalArea) { portalAreas.Add(portalArea); }
      if(child is Altar altar) { altars.Add(altar); }
    }
  }

  public override void _UnhandledInput(InputEvent @event) {
    if(@event is InputEventKey keyEvent) {
      if(keyEvent.Keycode == Key.Period) {
        playerInsanityComponent.AddInsanity(10.0f);
      }
      if(keyEvent.Keycode == Key.Comma) {
        playerInsanityComponent.AddInsanity(-10.0f);
      }
    }
  }

  private void OnPlayerDeath() {
    player.ProcessMode = ProcessModeEnum.Disabled;

    Timer deathTimer = new();
    AddChild(deathTimer);
    deathTimer.OneShot = true;
    deathTimer.Start(5.0);
    deathTimer.Timeout += () => {
      player.Reset();
      player.GlobalPosition = playerSpawn.GlobalPosition;
      player.GlobalRotation = playerSpawn.GlobalRotation;
      player.ProcessMode = ProcessModeEnum.Inherit;
    };
  }

  private void OnInsanityChanged(float insanity) {
    float intensityValue =
      Mathf.Remap(
        insanity,
        0.0f,
        playerInsanityComponent.MaxInsanity,
        0.0f,
        10.0f
      );

    Tween tween = CreateTween();
    _ = tween.TweenMethod(
      Callable.From(
        (float value) => skyShader.SetShaderParameter("accent_intensity", value)
      ),
      skyShader.GetShaderParameter("accent_intensity"),
      intensityValue,
      0.5
    );
  }

  private void OnInsanityLevelChanged(InsanityLevel level) {
    switch(level) {
      case InsanityLevel.Normal:
        foreach(PortalArea area in portalAreas) { area.chance = 0.0f; }
        break;
      case InsanityLevel.Medium:
        foreach(PortalArea area in portalAreas) { area.chance = 0.35f; }
        break;
      case InsanityLevel.High:
        foreach(PortalArea area in portalAreas) { area.chance = 0.7f; }
        break;
      default: break;
    }
  }
}

