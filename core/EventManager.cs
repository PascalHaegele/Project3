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
  private EnemyDifficultyInfo enemyDifficultyInfo = new();

  [ExportGroup("Enemy Difficulty")]
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float difficultyMultiplierNormal = 0.0f;
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float difficultyMultiplierMedium = 0.2f;
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float difficultyMultiplierHigh = 0.4f;

  [ExportGroup("Portal Activation Chances")]
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float portalChanceNormal = 0.0f;
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float portalChanceMedium = 0.35f;
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float portalChanceHigh = 0.7f;

  [ExportGroup("Trap Activation Chances")]
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float trapChanceNormal = 0.2f;
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float trapChanceMedium = 0.65f;
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float trapChanceHigh = 1.0f;

  [ExportGroup("Loop")]
  [Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
  private float loopDifficultyMultiplier = 0.05f;

  private float enemySpeedMultiplier = 1.0f;
  private float enemyHealthMultiplier = 1.0f;
  private float enemyDamageMultiplier = 1.0f;

  private readonly List<PortalArea> portalAreas = new();
  private readonly List<Portal> portals = new();

  private readonly List<TrapArea> trapAreas = new();

  private readonly List<Altar> altars = new();

  private FmodEvent ambientEvent;

  private Node3D currentMap;

  private int loopCount;

  public Node3D CurrentMap {
    set {
      currentMap = value;
      MapChange();
    }
  }

  [Signal]
  public delegate void PortalLevelChangeEventHandler(StringName newLevelPath);

  [Signal] public delegate void RestartEventHandler();

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

    ambientEvent = FmodServerWrapper.CreateEventInstance("event:/Ambient_Timeline");
    this.player.AddChild(ambientEvent);
    ambientEvent.Start();
    OnInsanityChanged(playerInsanityComponent.CurrentInsanity);
  }

  public void AddEnemy(Enemy enemy) {
    enemies.Add(enemy);

    enemy.difficultyInfo = enemyDifficultyInfo;

    _ = enemy.Connect(
      Enemy.SignalName.Killed,
      Callable.From<Enemy>(OnEnemyKilled),
      (uint)ConnectFlags.OneShot
    );
  }

  private void MapChange() {
    // Reset upgrade benches for the new level/run
    UpgradeBench.ResetAllBenches();

    // Reset all individual bench instances in the new map
    foreach (Node child in currentMap.GetChildren()) {
      if (child is UpgradeBench bench) {
        bench.ResetBenchInstance();
      }
    }

    environment = currentMap.GetNode<WorldEnvironment>("WorldEnvironment");
    skyShader = environment.Environment.Sky.SkyMaterial as ShaderMaterial;

    playerSpawn = currentMap.GetNode<Marker3D>("PlayerSpawn");

    enemies.Clear();
    portalAreas.Clear();
    portals.Clear();
    trapAreas.Clear();
    altars.Clear();

    Node? p = currentMap.GetNodeOrNull("Portals");
    Node? t = currentMap.GetNodeOrNull("Traps");
    Node? a = currentMap.GetNodeOrNull("Altars");

    if(p != null) {
      foreach(Node child in p.GetChildren()) {
        if(child is PortalArea portalArea) { portalAreas.Add(portalArea); }
        if(child is Portal portal) {
          portals.Add(portal);

          if(portal.isLoop) {
            portal.Loop += () => {
              loopCount++;
              GD.Print($"Loop: {loopCount}");
              UpdateDifficulty();
            };
          }

          if(portal.isLevelChange) {
            portal.ChangeLevel += EmitSignalPortalLevelChange;
          }
        }
      }
    }

    if(t != null) {
      foreach(Node child in t.GetChildren()) {
        if(child is TrapArea trapArea) { trapAreas.Add(trapArea); }
      }
    }

    if(a != null) {
      foreach(Node child in a.GetChildren()) {
        if(child is Altar altar) { altars.Add(altar); }
      }
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
    deathTimer.Start(4.0);
    deathTimer.Timeout += () => {
      currentMap.QueueFree();
      EmitSignalRestart();
    };
  }

  private void OnInsanityChanged(float insanity) {
    if(ambientEvent != null) {
      ambientEvent.SetParameterByName("Insanity", insanity);
    }

    if(playerInsanityComponent == null || skyShader == null) {
      return;
    }

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
        foreach(PortalArea area in portalAreas) {
          area.activationChance = portalChanceNormal;
        }
        foreach(TrapArea area in trapAreas) {
          area.activationChance = trapChanceNormal;
        }

        enemySpeedMultiplier = 1.0f + difficultyMultiplierNormal;
        enemyHealthMultiplier = 1.0f + difficultyMultiplierNormal;
        enemyDamageMultiplier = 1.0f + difficultyMultiplierNormal;

        break;
      case InsanityLevel.Medium:
        foreach(PortalArea area in portalAreas) {
          area.activationChance = portalChanceMedium;
        }
        foreach(TrapArea area in trapAreas) {
          area.activationChance = trapChanceMedium;
        }

        enemyDamageMultiplier = 1.0f + difficultyMultiplierMedium;
        enemyHealthMultiplier = 1.0f + difficultyMultiplierMedium;
        enemySpeedMultiplier = 1.0f + difficultyMultiplierMedium;

        break;
      case InsanityLevel.High:
        foreach(PortalArea area in portalAreas) {
          area.activationChance = portalChanceHigh;
        }
        foreach(TrapArea area in trapAreas) {
          area.activationChance = trapChanceHigh;
        }

        enemyDamageMultiplier = 1.0f + difficultyMultiplierHigh;
        enemyHealthMultiplier = 1.0f + difficultyMultiplierHigh;
        enemySpeedMultiplier = 1.0f + difficultyMultiplierHigh;

        break;
      default: break;
    }

    UpdateDifficulty();
  }

  private void UpdateDifficulty() {
    enemyDifficultyInfo.speedMultiplier =
      enemyDamageMultiplier + loopCount * loopDifficultyMultiplier;
    enemyDifficultyInfo.healthMultiplier =
      enemyDamageMultiplier + loopCount * loopDifficultyMultiplier;
    enemyDifficultyInfo.damageMultiplier =
      enemyDamageMultiplier + loopCount * loopDifficultyMultiplier;

    _ = enemyDifficultyInfo.EmitSignal(Resource.SignalName.Changed);

    GD.Print(
      $"Enemy Speed Multiplier: {enemyDifficultyInfo.speedMultiplier}, " +
      $"Enemy Health Multiplier: {enemyDifficultyInfo.healthMultiplier}, "+
      $"Enemy Damage Multiplier: {enemyDifficultyInfo.damageMultiplier}"
    );
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
