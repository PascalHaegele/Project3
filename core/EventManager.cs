using FmodSharp;
using Godot;
using System.Collections.Generic;

public partial class EventManager : Node {
  private Player player;
  private InsanityComponent playerInsanityComponent;

  private Marker3D playerSpawn;

  private WorldEnvironment environment;
  private ShaderMaterial skyShader;

  private readonly List<Enemy> enemies = new();

  private readonly List<PortalArea> portalAreas = new();
  private readonly List<Portal> portals = new();

  private readonly List<Altar> altars = new();

  private FmodEvent ambientEvent;

  private Node3D currentMap;

  public Node3D CurrentMap {
    set {
      currentMap = value;
      MapChange();
    }
  }

  [Signal]
  public delegate void PortalLevelChangeEventHandler(StringName newLevelPath);

  public override void _Ready() {
    if(GetNodeOrNull("HitstopManager") == null) {
      AddChild(new HitstopManager());
    }

    GetTree().NodeAdded += (node) => {
      if(node is Enemy enemy) { AddEnemy(enemy); }
    };
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

  public void SetPlayer(Player player) {
    this.player = player;
    playerInsanityComponent = this.player.GetComponent<InsanityComponent>();

    playerInsanityComponent.InsanityChanged += OnInsanityChanged;
    playerInsanityComponent.InsanityLevelChanged += OnInsanityLevelChanged;

    this.player.GetComponent<HealthComponent>().Died += OnPlayerDeath;

    ambientEvent =
      FmodServerWrapper.CreateEventInstance("event:/Walk_Timeline");
    this.player.AddChild(ambientEvent);
    ambientEvent.Start();
  }

  public void AddEnemy(Enemy enemy) {
    enemies.Add(enemy);

    _ = enemy.Connect(
      Enemy.SignalName.Killed,
      Callable.From<Enemy>(OnEnemyKilled),
      (uint)ConnectFlags.OneShot
    );
  }

  private void MapChange() {
    environment = currentMap.GetNode<WorldEnvironment>("WorldEnvironment");
    skyShader = environment.Environment.Sky.SkyMaterial as ShaderMaterial;

    playerSpawn = currentMap.GetNode<Marker3D>("PlayerSpawn");

    enemies.Clear();
    portalAreas.Clear();
    portals.Clear();
    altars.Clear();
    foreach(Node child in currentMap.GetChildren()) {
      if(child is PortalArea portalArea) { portalAreas.Add(portalArea); }
      if(child is Portal portal) {
        portals.Add(portal);
        if(portal.isLevelChange) {
          portal.ChangeLevel += EmitSignalPortalLevelChange;
        }
      }
      if(child is Altar altar) { altars.Add(altar); }
    }

    OnInsanityChanged(playerInsanityComponent.CurrentInsanity);
    OnInsanityLevelChanged(playerInsanityComponent.CurrentLevel);
  }

  private void OnEnemyKilled(Enemy enemy) {
    if(enemies.Remove(enemy)) {
      enemy.Killed -= OnEnemyKilled;
      playerInsanityComponent.AddInsanity(10.0f);
      player.GetComponent<HealthComponent>().OnEliteKill();
    }
  }

  private void StartAmbientSound(string eventPath) {
    ambientEvent = FmodServerWrapper.CreateEventInstance(eventPath);
    _ = ambientEvent?.Call("start");
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

    // Reiner nativer Parameter-Aufruf an das GDExtension-Event
    // if(_ambientInstance != null && IsInstanceValid(_ambientInstance)) {
    //   _ambientInstance.Call("set_parameter", "Insanity", insanity);
    // }
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

  // Stoppt den Sound beim Szenenwechsel sauber und gibt den RAM frei
  protected override void Dispose(bool disposing) {
    if(ambientEvent != null) {
      ambientEvent.Stop(FmodServerWrapper.FMOD_STUDIO_STOP_ALLOWFADEOUT);
      ambientEvent.Release();
    }
    base.Dispose(disposing);
  }
}
