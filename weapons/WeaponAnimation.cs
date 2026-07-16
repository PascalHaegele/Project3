using Godot;

/// <summary>
/// Erweiterte Waffen-Animation: Recoil, Sway, Spring-Trägheit,
/// sowie Sprint/Jump/Reload/Dash-Animationen.
/// Alle Werte sind im Inspektor editierbar.
/// </summary>
[GlobalClass]
public partial class WeaponAnimation : Node3D {
  private Node3D weaponNode;
  private Tween recoilTween;
  private Tween actionTween;
  private bool isRecoiling;
  private bool isActionPlaying;

  // =====================================================================
  //  INSPEKTOR: ALLE ANIMATIONS-PARAMETER
  // =====================================================================

  // --- Recoil ---
  [ExportGroup("Recoil")]
  [Export] private float recoilStrength = 0.045f;
  [Export] private float recoilAngle = -0.045f;
  [Export] private float recoilSideDriftMax = 0.015f;
  [Export] private float recoilVariation = 0.1f;
  [Export] private float recoilKickDuration = 0.02f;
  [Export] private float recoilReturnDuration = 0.07f;
  [Export] private float recoilPauseDuration = 0.01f;

  // --- Reload ---
  [ExportGroup("Reload")]
  [Export] private float reloadDropX = 0.15f;
  [Export] private float reloadDropY = -0.25f;
  [Export] private float reloadDropZ = 0.12f;
  [Export] private float reloadRotX = -0.4f;
  [Export] private float reloadRotY = 0.6f;
  [Export] private float reloadRotZ = 0.2f;
  [Export] private float reloadDropDuration = 0.1f;
  [Export] private float reloadHoldDuration = 0.2f;
  [Export] private float reloadReturnDuration = 0.12f;

  // --- Sprint ---
  [ExportGroup("Sprint")]
  [Export] private float sprintOffsetY = -0.025f;
  [Export] private float sprintOffsetZ = 0.01f;
  [Export] private float sprintTiltX = -0.02f;
  [Export] private float sprintDuration = 0.15f;

  // --- Jump ---
  [ExportGroup("Jump")]
  [Export] private float jumpOffsetY = 0.015f;
  [Export] private float jumpOffsetZ = -0.005f;
  [Export] private float jumpTiltX = 0.015f;
  [Export] private float jumpUpDuration = 0.06f;
  [Export] private float jumpHoldDuration = 0.08f;
  [Export] private float jumpDownDuration = 0.08f;

  // --- Dash ---
  [ExportGroup("Dash")]
  [Export] private float dashOffsetX = 0.025f;
  [Export] private float dashOffsetY = -0.01f;
  [Export] private float dashOffsetZ = 0.01f;
  [Export] private float dashTiltX = -0.02f;
  [Export] private float dashRollZ = 0.06f;
  [Export] private float dashOutDuration = 0.07f;
  [Export] private float dashHoldDuration = 0.05f;
  [Export] private float dashReturnDuration = 0.12f;

  // --- Sway ---
  [ExportGroup("Sway")]
  [Export] private float swaySmoothing = 6.0f;
  [Export] private float swayMax = 0.005f;

  // --- Spring / Trägheit ---
  [ExportGroup("Spring")]
  [Export] private float springStiffness = 50.0f;
  [Export] private float springDamping = 20.0f;

  // =====================================================================
  //  INTERNE DATEN
  // =====================================================================
  private Vector2 swayTarget;
  private Vector2 swayCurrent;
  private Vector3 springPos;
  private Vector3 springVel;
  private Vector3 lastGlobalPos;
  private Vector3 originalPos;
  private Vector3 originalRot;

  public override void _Ready() {
    weaponNode = GetParent<Node3D>();
    originalPos = weaponNode.Position;
    originalRot = weaponNode.Rotation;
    lastGlobalPos = weaponNode.GlobalPosition;
  }

  public override void _Process(double delta) {
    float dt = (float)delta;
    UpdateSway(dt);
    UpdateSpring(dt);
    ApplyTransform();
  }

  // =====================================================================
  //  PUBLIC: Shoot / Recoil
  // =====================================================================
  public void PlayRecoil() {
    if(weaponNode == null) { return; }
    ResetToOriginal();
    recoilTween?.Kill();
    recoilTween = CreateTween();
    isRecoiling = true;
    recoilTween.Finished += () => isRecoiling = false;

    float strengthVar = (float)GD.RandRange(1.0f - recoilVariation, 1.0f + recoilVariation);
    float sideDrift = (float)GD.RandRange(-recoilSideDriftMax, recoilSideDriftMax);

    Vector3 kickPos = new Vector3(0.0f, 0.0f, recoilStrength * strengthVar);
    Vector3 kickRot = new Vector3(recoilAngle * strengthVar, sideDrift, 0.0f);

    recoilTween.Parallel().TweenProperty(weaponNode, "position", originalPos + kickPos, recoilKickDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    recoilTween.Parallel().TweenProperty(weaponNode, "rotation", originalRot + kickRot, recoilKickDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

    recoilTween.TweenInterval(recoilPauseDuration);
    recoilTween.Parallel().TweenProperty(weaponNode, "position", originalPos, recoilReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
    recoilTween.Parallel().TweenProperty(weaponNode, "rotation", originalRot, recoilReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
  }

  // =====================================================================
  //  PUBLIC: Reload
  // =====================================================================
  public void PlayReload() {
    if(weaponNode == null) { return; }
    StopAction();
    ResetToOriginal();
    actionTween = CreateTween();
    isActionPlaying = true;
    actionTween.Finished += () => isActionPlaying = false;

    Vector3 dropPos = originalPos + new Vector3(reloadDropX, reloadDropY, reloadDropZ);
    Vector3 dropRot = originalRot + new Vector3(reloadRotX, reloadRotY, reloadRotZ);

    actionTween.Parallel().TweenProperty(weaponNode, "position", dropPos, reloadDropDuration)
      .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", dropRot, reloadDropDuration)
      .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);

    actionTween.TweenInterval(reloadHoldDuration);

    actionTween.Parallel().TweenProperty(weaponNode, "position", originalPos, reloadReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", originalRot, reloadReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
  }

  // =====================================================================
  //  PUBLIC: Sprint
  // =====================================================================
  public void PlaySprint(bool active) {
    if(weaponNode == null) { return; }
    StopAction();
    ResetToOriginal();
    if(!active) { return; }

    actionTween = CreateTween();
    isActionPlaying = true;
    actionTween.Finished += () => isActionPlaying = false;

    Vector3 sprintPos = originalPos + new Vector3(0.0f, sprintOffsetY, sprintOffsetZ);
    Vector3 sprintRot = originalRot + new Vector3(sprintTiltX, 0.0f, 0.0f);

    actionTween.Parallel().TweenProperty(weaponNode, "position", sprintPos, sprintDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", sprintRot, sprintDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
  }

  // =====================================================================
  //  PUBLIC: Jump
  // =====================================================================
  public void PlayJump() {
    if(weaponNode == null) { return; }
    StopAction();
    ResetToOriginal();
    actionTween = CreateTween();
    isActionPlaying = true;
    actionTween.Finished += () => isActionPlaying = false;

    Vector3 jumpPos = originalPos + new Vector3(0.0f, jumpOffsetY, jumpOffsetZ);
    Vector3 jumpRot = originalRot + new Vector3(jumpTiltX, 0.0f, 0.0f);

    actionTween.Parallel().TweenProperty(weaponNode, "position", jumpPos, jumpUpDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", jumpRot, jumpUpDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

    actionTween.TweenInterval(jumpHoldDuration);
    actionTween.Parallel().TweenProperty(weaponNode, "position", originalPos, jumpDownDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", originalRot, jumpDownDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
  }

  // =====================================================================
  //  PUBLIC: Dash
  // =====================================================================
  public void PlayDash() {
    if(weaponNode == null) { return; }
    StopAction();
    ResetToOriginal();
    actionTween = CreateTween();
    isActionPlaying = true;
    actionTween.Finished += () => isActionPlaying = false;

    float side = (float)GD.RandRange(0.0f, 1.0f) > 0.5f ? 1.0f : -1.0f;

    Vector3 dashPos = originalPos + new Vector3(side * dashOffsetX, dashOffsetY, dashOffsetZ);
    Vector3 dashRot = originalRot + new Vector3(dashTiltX, 0.0f, side * dashRollZ);

    actionTween.Parallel().TweenProperty(weaponNode, "position", dashPos, dashOutDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", dashRot, dashOutDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

    actionTween.TweenInterval(dashHoldDuration);
    actionTween.Parallel().TweenProperty(weaponNode, "position", originalPos, dashReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", originalRot, dashReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
  }

  // =====================================================================
  //  HELPER
  // =====================================================================
  private void ResetToOriginal() {
    weaponNode.Position = originalPos;
    weaponNode.Rotation = originalRot;
  }

  private void StopAction() {
    actionTween?.Kill();
    isActionPlaying = false;
  }

  public bool IsBusy() => isRecoiling || isActionPlaying;

  // =====================================================================
  //  PRIVATE: Sway
  // =====================================================================
  private void UpdateSway(float dt) {
    if(Input.MouseMode == Input.MouseModeEnum.Captured) {
      Vector2 mouseVel = Input.GetLastMouseVelocity();
      swayTarget = new Vector2(
        Mathf.Clamp(-mouseVel.X * 0.00005f, -swayMax, swayMax),
        Mathf.Clamp(-mouseVel.Y * 0.00005f, -swayMax, swayMax)
      );
    } else {
      swayTarget = Vector2.Zero;
    }
    swayCurrent = swayCurrent.Lerp(swayTarget, swaySmoothing * dt);
    if(swayCurrent.LengthSquared() < 0.0000001f) { swayCurrent = Vector2.Zero; }
  }

  // =====================================================================
  //  PRIVATE: Spring
  // =====================================================================
  private void UpdateSpring(float dt) {
    if(weaponNode == null) { return; }
    Vector3 worldVel = (weaponNode.GlobalPosition - lastGlobalPos) / Mathf.Max(dt, 0.0001f);
    lastGlobalPos = weaponNode.GlobalPosition;
    Vector3 localVel = weaponNode.ToLocal(weaponNode.GlobalPosition + worldVel) - weaponNode.ToLocal(weaponNode.GlobalPosition);
    Vector3 force = -springStiffness * springPos - springDamping * springVel + localVel * springStiffness * 0.1f;
    springVel += force * dt;
    springPos += springVel * dt;
    springPos = springPos.Clamp(new Vector3(-0.008f, -0.008f, -0.008f), new Vector3(0.008f, 0.008f, 0.008f));
  }

  // =====================================================================
  //  APPLY
  // =====================================================================
  private void ApplyTransform() {
    if(weaponNode == null) { return; }
    if(isRecoiling || isActionPlaying) { return; }
    Vector3 totalOffset = new Vector3(swayCurrent.X, swayCurrent.Y, 0.0f) + springPos;
    Vector3 totalRot = new Vector3(
      swayCurrent.Y * 0.5f + springPos.Z * 1.5f,
      swayCurrent.X * 0.3f + springPos.X * 1.0f,
      0.0f
    );
    if(totalOffset.LengthSquared() < 0.0000001f && totalRot.LengthSquared() < 0.0000001f) { return; }
    weaponNode.Position = originalPos + totalOffset;
    weaponNode.Rotation = originalRot + totalRot;
  }
}