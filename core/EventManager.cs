using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class EventManager : Node {
  private Player player;
  private InsanityComponent playerInsanityComponent;

  private Marker3D playerSpawn;

  private WorldEnvironment environment;
  private ShaderMaterial skyShader;

  private readonly List<PortalArea> portalAreas = new();
  private readonly List<Altar> altars = new();

  // Da wir nativ über die GDExtension gehen, ist die Instanz ein rohes GodotObject
  private GodotObject _ambientInstance; 

  public override async void _Ready() {
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

    // Wartet 1 Frame, bis das FMOD C++ Plugin im Hintergrund voll hochgefahren ist
    await Task.Yield();
    // ÄNDERN SIE DIESE ZEILE:
  StartAmbientSound("event:Ambient_Timeline");

  }
private void StartAmbientSound(string eventPath) {
  var fmodServer = Engine.GetSingleton("FmodServer");
  if (fmodServer != null) {
    
    // Deine exakte GUID aus FMOD Studio
    string eventGuid = "{22611b6c-032c-4b1b-a497-3a42ec10800a}";

    // BEHEBT DEN ABSTURZ: Wir nutzen die offizielle GUID-Methode des Plugins!
    // Dadurch parst das C++ Plugin den String intern korrekt als ID.
    _ambientInstance = fmodServer.Call("create_event_instance_with_guid", eventGuid).As<GodotObject>();
    
    if (_ambientInstance != null && GodotObject.IsInstanceValid(_ambientInstance)) {
      _ambientInstance.Call("start");
      GD.Print("FMOD GDExtension: Ambient Sound erfolgreich über GUID gestartet!");
    } else {
      GD.PrintErr("FMOD Fehler: Instanz konnte über die GUID nicht erstellt werden. Sind die Banks im Ordner FmodBank/Desktop?");
    }
  } else {
    GD.PrintErr("FMOD Fehler: FmodServer-Singleton nicht gefunden!");
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

    // Reiner nativer Parameter-Aufruf an das GDExtension-Event
    if (_ambientInstance != null && GodotObject.IsInstanceValid(_ambientInstance)) {
      _ambientInstance.Call("set_parameter", "Insanity", insanity);
    }
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
    if (_ambientInstance != null && GodotObject.IsInstanceValid(_ambientInstance)) {
      _ambientInstance.Call("stop", 0); // 0 = FMOD_STUDIO_STOP_ALLOW_FADEOUT
      _ambientInstance.Call("release");
    }
    base.Dispose(disposing);
  }
}
