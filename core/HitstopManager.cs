using Godot;

public partial class HitstopManager : Node {
  public static HitstopManager Instance { get; private set; }
  private float timer;
  private float originalTimeScale = 1.0f;

  public override void _Ready() {
    Instance = this;
    originalTimeScale = (float)Engine.TimeScale;
  }

  public override void _Process(double delta) {
    if(timer > 0.0f) {
      timer -= (float)delta;
      if(timer <= 0.0f) {
        Engine.TimeScale = originalTimeScale;
      }
    }
  }

  public void Trigger(float duration, float timeScale = 0.05f) {
    if(duration <= 0.0f || timeScale <= 0.0f) { return; }
    timer = duration;
    Engine.TimeScale = timeScale;
  }
}