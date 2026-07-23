using Godot;

public partial class HealingAnimation : Node3D {
  private Node3D weaponNode;
  private Camera3D camera;
  private Node3D potionModel;
  private Marker3D potionHoldPoint;
  private Vector3 originalWeaponPos;
  private Vector3 originalWeaponRot;
  private Vector3 originalCamPos;
  private Vector3 originalCamRot;
  private bool isPlaying;

  [Signal] public delegate void HealingCompleteEventHandler();

  public override void _Ready() {
    weaponNode = GetNode<Node3D>("../Revolver");
    camera = GetNode<Camera3D>("../CameraComponent");

    // Create a potion hold point (left hand area)
    potionHoldPoint = new Marker3D();
    potionHoldPoint.Name = "PotionHoldPoint";
    AddChild(potionHoldPoint);

    // Load the potion model
    PackedScene potionScene = GD.Load<PackedScene>("res://objects/items/Potion.glb");
    if(potionScene != null) {
      potionModel = potionScene.Instantiate<Node3D>();
      potionHoldPoint.AddChild(potionModel);
      // Default scale to match world pickup size
      potionModel.Scale = new Vector3(0.08f, 0.08f, 0.08f);
    }

    Visible = false;
  }

  public void PlayHeal() {
    if(isPlaying || weaponNode == null || potionModel == null) return;

    isPlaying = true;
    originalWeaponPos = weaponNode.Position;
    originalWeaponRot = weaponNode.Rotation;
    originalCamPos = camera.Position;
    originalCamRot = camera.Rotation;

    Visible = true;
    if(potionModel != null) potionModel.Visible = true;

    // Start position: far below-left of screen (left hand start)
    potionHoldPoint.Position = new Vector3(-0.5f, -0.6f, -0.1f);
    potionHoldPoint.Rotation = new Vector3(0.0f, 0.0f, -1.5f);
    potionHoldPoint.Scale = new Vector3(1, 1, 1);

    // Phase 1: Start - weapon lowers slightly, camera dips (0.08s)
    Tween t1 = CreateTween();
    t1.SetParallel(true);
    t1.TweenProperty(weaponNode, "position", originalWeaponPos + new Vector3(-0.02f, -0.03f, 0.01f), 0.08f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    t1.TweenProperty(weaponNode, "rotation", originalWeaponRot + new Vector3(0.02f, 0.0f, -0.01f), 0.08f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    t1.TweenProperty(camera, "position", originalCamPos + new Vector3(0.0f, -0.01f, 0.005f), 0.08f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

    t1.Finished += () => {
      // Phase 2: Grab - hand brings potion up from below-left (0.15s)
      Tween t2 = CreateTween();
      t2.SetParallel(true);
      t2.TweenProperty(potionHoldPoint, "position", new Vector3(-0.35f, -0.15f, -0.15f), 0.15f)
        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
      t2.TweenProperty(potionHoldPoint, "rotation", new Vector3(0.3f, 0.3f, -0.4f), 0.15f)
        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
      // Weapon shifts slightly
      t2.TweenProperty(weaponNode, "position", originalWeaponPos + new Vector3(-0.03f, -0.02f, 0.01f), 0.15f)
        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
      t2.TweenProperty(weaponNode, "rotation", originalWeaponRot + new Vector3(0.02f, 0.01f, -0.02f), 0.15f)
        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

      t2.Finished += () => {
        // Phase 3: Anticipation - slight pull back (0.08s)
        Tween t3 = CreateTween();
        t3.SetParallel(true);
        t3.TweenProperty(potionHoldPoint, "position", new Vector3(-0.30f, -0.10f, -0.18f), 0.08f)
          .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        t3.TweenProperty(potionHoldPoint, "rotation", new Vector3(0.15f, 0.15f, -0.2f), 0.08f)
          .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

        t3.Finished += () => {
          // Phase 4: Drink - potion to mouth area (0.15s + 0.4s hold)
          Tween t4 = CreateTween();
          t4.SetParallel(true);
          t4.TweenProperty(potionHoldPoint, "position", new Vector3(-0.12f, 0.03f, -0.28f), 0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
          t4.TweenProperty(potionHoldPoint, "rotation", new Vector3(-0.3f, 0.0f, 0.0f), 0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
          // Camera tilt up
          t4.TweenProperty(camera, "rotation", originalCamRot + new Vector3(0.02f, 0.0f, 0.0f), 0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
          
          t4.TweenInterval(0.4f);

          // Drink bob (during hold)
          Tween drinkBob = CreateTween().SetParallel(true);
          drinkBob.TweenProperty(potionHoldPoint, "position", new Vector3(-0.12f, 0.04f, -0.28f), 0.2f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
          drinkBob.TweenInterval(0.2f);
          drinkBob.TweenProperty(potionHoldPoint, "position", new Vector3(-0.12f, 0.02f, -0.28f), 0.2f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

          t4.Finished += () => {
            // Phase 5: Finish - final movement (0.1s)
            Tween t5 = CreateTween();
            t5.SetParallel(true);
            t5.TweenProperty(potionHoldPoint, "position", new Vector3(-0.14f, 0.05f, -0.24f), 0.1f)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            t5.TweenProperty(potionHoldPoint, "rotation", new Vector3(-0.1f, 0.0f, -0.05f), 0.1f)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            
            t5.Finished += () => {
              // Phase 6: Throw away far left (0.12s)
              Tween t6 = CreateTween();
              t6.SetParallel(true);
              t6.TweenProperty(potionHoldPoint, "position", new Vector3(-0.6f, 0.2f, -0.4f), 0.12f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
              t6.TweenProperty(potionHoldPoint, "rotation", new Vector3(2.0f, -1.0f, -3.0f), 0.12f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
              t6.TweenProperty(potionHoldPoint, "scale", new Vector3(0.01f, 0.01f, 0.01f), 0.12f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);

              t6.Finished += () => {
                if(potionModel != null) potionModel.Visible = false;
                potionHoldPoint.Scale = new Vector3(1, 1, 1);

                // Phase 7: Return weapon + camera (0.15s)
                Tween t7 = CreateTween();
                t7.SetParallel(true);
                t7.TweenProperty(weaponNode, "position", originalWeaponPos, 0.15f)
                  .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
                t7.TweenProperty(weaponNode, "rotation", originalWeaponRot, 0.15f)
                  .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
                t7.TweenProperty(camera, "position", originalCamPos, 0.15f)
                  .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
                t7.TweenProperty(camera, "rotation", originalCamRot, 0.15f)
                  .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

                t7.Finished += () => {
                  isPlaying = false;
                  Visible = false;
                  EmitSignalHealingComplete();
                };
              };
            };
          };
        };
      };
    };
  }

  public bool IsPlaying() => isPlaying;
}