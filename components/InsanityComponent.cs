using Godot;

public enum InsanityLevel {
  Normal,
  Medium,
  High
}

[GlobalClass]
public partial class InsanityComponent : Node {
  // =========================
  // Settings
  // =========================

  [ExportGroup("Insanity Settings")]

  [Export] public float MaxInsanity = 150f;
  [Export] public float MediumThreshold = 50f;
  [Export] public float HighThreshold = 100f;

  // =========================
  // Runtime
  // =========================

  public float CurrentInsanity { get; private set; } = 0f;

  public InsanityLevel CurrentLevel { get; private set; } = InsanityLevel.Normal;

  // =========================
  // Signals
  // =========================

  [Signal] public delegate void InsanityChangedEventHandler(float insanity);

  [Signal]
  public delegate void InsanityLevelChangedEventHandler(InsanityLevel level);

  // =========================

  public override void _Ready() {
    UpdateInsanityLevel();
  }

  // =========================
  // Public Functions
  // =========================

  public void AddInsanity(float amount) {
    CurrentInsanity = Mathf.Clamp(CurrentInsanity + amount, 0f, MaxInsanity);

    GD.Print($"Insanity: {CurrentInsanity}");

    EmitSignalInsanityChanged(CurrentInsanity);

    UpdateInsanityLevel();
  }

  public void RemoveInsanity(float amount) {
    CurrentInsanity = Mathf.Clamp(CurrentInsanity - amount, 0f, MaxInsanity);

    GD.Print($"Insanity: {CurrentInsanity}");

    EmitSignalInsanityChanged(CurrentInsanity);

    UpdateInsanityLevel();
  }

  public void ResetInsanity() {
    CurrentInsanity = 0;

    EmitSignalInsanityChanged(CurrentInsanity);

    UpdateInsanityLevel();
  }

  // =========================
  // Private
  // =========================

  private void UpdateInsanityLevel() {
    InsanityLevel previousLevel = CurrentLevel;

    CurrentLevel =
      CurrentInsanity < MediumThreshold ?
      InsanityLevel.Normal :
      CurrentInsanity < HighThreshold ?
      InsanityLevel.Medium :
      InsanityLevel.High;

    if(previousLevel != CurrentLevel) {
      GD.Print("");
      GD.Print("==================================");
      GD.Print($"INSANITY LEVEL -> {CurrentLevel}");
      GD.Print("==================================");

      EmitSignalInsanityLevelChanged(CurrentLevel);

      // PrintCurrentEffects();
    }
  }

  // =========================
  // Debug Preview
  // =========================

  private void PrintCurrentEffects() {
    switch(CurrentLevel) {
      case InsanityLevel.Normal:

        GD.Print("Gameplay");
        GD.Print("- Normal dungeon layout");
        GD.Print("- Standard enemy variants");
        GD.Print("- Standard loot");
        GD.Print("- Stable reality");

        GD.Print("");

        GD.Print("Visual");
        GD.Print("- Normal lighting");
        GD.Print("- No visual distortion");

        GD.Print("");

        GD.Print("Audio");
        GD.Print("- Normal ambience");

        break;

      case InsanityLevel.Medium:

        GD.Print("Gameplay");
        GD.Print("- Secret Rooms");
        GD.Print("- Hidden Passages");
        GD.Print("- Environment Changes");
        GD.Print("- Corrupted Enemies");
        GD.Print("- Increased Enemy Damage");
        GD.Print("- Increased Enemy Health");

        GD.Print("");

        GD.Print("Visual");
        GD.Print("- Screen Distortion");
        GD.Print("- Flickering Lights");
        GD.Print("- Paintings Watching");
        GD.Print("- Distorted Mirrors");
        GD.Print("- Moving Shadows");
        GD.Print("- Objects Change Appearance");

        GD.Print("");

        GD.Print("Audio");
        GD.Print("- Footsteps Behind Player");
        GD.Print("- Random Whispers");
        GD.Print("- Directional Audio Illusions");

        break;

      case InsanityLevel.High:

        GD.Print("Gameplay");
        GD.Print("- Dynamic Dungeon");
        GD.Print("- New Enemy Variants");
        GD.Print("- Elite Enemy Chance");
        GD.Print("- Boss Spawn Chance");
        GD.Print("- New Enemy Abilities");
        GD.Print("- Hidden Areas");
        GD.Print("- Reality Unstable");

        GD.Print("");

        GD.Print("Visual");
        GD.Print("- Entire Rooms Transform");
        GD.Print("- Impossible Architecture");
        GD.Print("- Endless Hallways");
        GD.Print("- Looping Staircases");
        GD.Print("- Fake Walls");
        GD.Print("- Environment Shifts");

        GD.Print("");

        GD.Print("Audio");
        GD.Print("- Heavy Hallucinations");
        GD.Print("- Distorted Music");
        GD.Print("- Multiple Whisper Sources");
        GD.Print("- Fake Enemy Sounds");
        GD.Print("- Complete Silence");

        break;
    }

    GD.Print("");
  }
}
