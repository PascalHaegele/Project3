using Godot;

/// <summary>
/// Juicy FPS camera/weapon animation: idle, bob, recoil, look drag,
/// landing impact and smoother transitions. Designed for slow dark fantasy.
/// Inspector parameters are still tweakable per weapon.
/// </summary>
[GlobalClass]
public partial class WeaponAnimation : Node3D {
  private Node3D weaponNode;
  private OmniLight3D muzzleFlash;
  private Tween actionTween;
  private bool isActionPlaying;

  // Events
  [Signal] public delegate void ReloadVisualCompleteEventHandler();
  [Signal] public delegate void MuzzleFlashEventHandler(Vector3 globalPosition, Vector3 forward);
  [Signal] public delegate void CameraShakeEventHandler(float amount, float duration);
  [Signal] public delegate void CameraRecoilEventHandler(float amount);

  // --- Info from outside -->
  private Vector2 mouseVelocity;
  private float horizontalSpeed;
  private bool isSprinting;
  private bool isGrounded;
  private float verticalVelocity;
  private bool wasGroundedLastFrame = true;
  private float landedTimer;

  // ============================================================
  //  INSPEKTOR PARAMETERS
  // ============================================================

  // General smoothing / time scale
  [ExportGroup("Smoothing")]
  [Export] private float positionSmoothSpeed = 18.0f;
  [Export] private float rotationSmoothSpeed = 18.0f;
  [Export] private float lookInertiaStrength = 0.08f;
  [Export] private float lookInertiaDamping = 8.0f;

  // Idle breathing
  [ExportGroup("Idle")]
  [Export] private float idleBreathAmplitude = 0.0012f;
  [Export] private float idleBreathFrequency = 1.1f;
  [Export] private float idleTiltAmplitude = 0.025f;
  [Export] private float idleTiltFrequency = 1.1f;

  // Motion bob
  [ExportGroup("Movement Bob")]
  [Export] private float bobWalkAmplitude = 0.006f;
  [Export] private float bobSprintAmplitude = 0.010f;
  [Export] private float bobFrequency = 9.0f;
  [Export] private float bobSwayAmplitude = 0.012f;
  [Export] private float bobTiltPitch = 0.06f;
  [Export] private float bobTiltYaw = 0.10f;
  [Export] private float bobSmoothBlend = 10.0f;

  // Acceleration inertia
  [ExportGroup("Acceleration")]
  [Export] private float accelSpringStiffness = 55.0f;
  [Export] private float accelSpringDamping = 18.0f;
  [Export] private float accelFeedbackScale = 1.2f;
  [Export] private float accelClamp = 0.012f;

  // Recoil (spring-based, stackable)
  [ExportGroup("Recoil")]
  [Export] private float recoilKickZ = 0.28f;
  [Export] private float recoilKickPitch = 0.40f;
  [Export] private float recoilSideDriftMax = 0.08f;
  [Export] private float recoilVariation = 0.4f;
  [Export] private float recoilSpringStiffness = 180.0f;
  [Export] private float recoilSpringDamping = 10.0f;
  [Export] private float cameraShakeAmount = 0.018f;
  [Export] private float cameraShakeDuration = 0.08f;

  // Landing
  [ExportGroup("Landing")]
  [Export] private float landDipY = -0.04f;
  [Export] private float landDuration = 0.18f;

  // Reload
  [ExportGroup("Reload")]
  [Export] private float reloadDropX = 0.18f;
  [Export] private float reloadDropY = -0.28f;
  [Export] private float reloadDropZ = 0.15f;
  [Export] private float reloadRotX = -0.20f;
  [Export] private float reloadRotY = 0.25f;
  [Export] private float reloadRotZ = 0.08f;
  [Export] private float reloadDropDuration = 0.10f;
  [Export] private float reloadHoldDuration = 0.20f;
  [Export] private float reloadReturnDuration = 0.14f;

  // Sprint
  [ExportGroup("Sprint")]
  [Export] private float sprintOffsetY = -0.04f;
  [Export] private float sprintOffsetZ = 0.02f;
  [Export] private float sprintTiltX = -0.05f;
  [Export] private float sprintDuration = 0.12f;

  // Jump
  [ExportGroup("Jump")]
  [Export] private float jumpOffsetY = 0.025f;
  [Export] private float jumpOffsetZ = -0.01f;
  [Export] private float jumpTiltX = 0.025f;
  [Export] private float jumpUpDuration = 0.06f;
  [Export] private float jumpHoldDuration = 0.08f;
  [Export] private float jumpDownDuration = 0.08f;

  // Dash
  [ExportGroup("Dash")]
  [Export] private float dashOffsetX = 0.03f;
  [Export] private float dashOffsetY = -0.015f;
  [Export] private float dashOffsetZ = 0.015f;
  [Export] private float dashTiltX = -0.03f;
  [Export] private float dashRollZ = 0.08f;
  [Export] private float dashOutDuration = 0.07f;
  [Export] private float dashHoldDuration = 0.05f;
  [Export] private float dashReturnDuration = 0.12f;

  // ============================================================
  //  STATE
  // ============================================================
  private Vector3 originalPos;
  private Vector3 originalRot;
  private Vector3 targetPos;
  private Vector3 targetRot;
  private Vector3 positionVel;
  private Vector3 rotationVel;

  // Recoil spring state
  private Vector3 recoilPos;
  private Vector3 recoilVel;
  private Vector3 recoilRot;
  private Vector3 recoilRotVel;

  private float bobT;
  private float bobTargetSmooth;
  private Vector2 mouseVelCurrent;
  private Vector2 mouseVelTarget;
  private Vector3 lastGlobalPos;

  // ============================================================
  //  LIFECYCLE
  // ============================================================
  public override void _Ready() {
    weaponNode = GetParent<Node3D>();
    originalPos = weaponNode.Position;
    originalRot = weaponNode.Rotation;
    targetPos = originalPos;
    targetRot = originalRot;
    lastGlobalPos = weaponNode.GlobalPosition;

    muzzleFlash = GetNodeOrNull<OmniLight3D>("MuzzleFlash");
    if(muzzleFlash != null) { muzzleFlash.Visible = false; }
  }

  public override void _Process(double delta) {
    float dt = (float)delta;

    UpdateLookInertia(dt);
    UpdateBob(dt);
    UpdateAccelerationSpring(dt);
    UpdateLanding(dt);

    // Combine layered offsets -> final target
    Vector3 extraPos = Vector3.Zero;
    Vector3 extraRot = Vector3.Zero;

    // 1) Idle breathing (always present when no action tween)
    if(!isActionPlaying) {
      float breath = Mathf.Sin(dt * idleBreathFrequency * Mathf.Tau) * idleBreathAmplitude;
      float breathZ = Mathf.Cos(dt * idleBreathFrequency * Mathf.Tau) * idleBreathAmplitude;
      extraPos.Y += breath;
      extraPos.Z += breathZ;
      float tilt = Mathf.Sin(dt * idleTiltFrequency * Mathf.Tau) * idleTiltAmplitude;
      extraRot.X += tilt;
    }

    // 2) Bob
    float bobAmp = isSprinting ? bobSprintAmplitude : bobWalkAmplitude;
    float samp = Mathf.Sin(bobT) * bobAmp;
    float samp2 = Mathf.Cos(bobT) * bobAmp * 0.6f;
    extraPos.Y += samp;
    extraPos.X += samp2 * bobSwayAmplitude;
    extraRot.Z += samp2 * bobTiltYaw;
    extraRot.X += Mathf.Sin(bobT * 0.5f) * bobTiltPitch * 0.3f;

    // 3) Look inertia (add to rotation)
    extraRot.X += mouseVelCurrent.Y * lookInertiaStrength;
    extraRot.Y += mouseVelCurrent.X * lookInertiaStrength;
    extraRot.Z += -mouseVelCurrent.X * lookInertiaStrength * 0.15f;

    targetPos = originalPos + extraPos;
    targetRot = originalRot + extraRot;

    SmoothApply(dt);
  }

  // ============================================================
  //  PUBLIC API FOR EXTERNAL STATE
  // ============================================================
  public void SetLookVelocity(Vector2 velocity) {
    mouseVelocity = velocity;
  }

  public void SetMotionState(float speed, bool sprinting) {
    horizontalSpeed = speed;
    isSprinting = sprinting;
  }

  public void SetGrounded(bool grounded, float vy) {
    wasGroundedLastFrame = isGrounded;
    isGrounded = grounded;
    verticalVelocity = vy;
    if(wasGroundedLastFrame && !isGrounded) {
      // left ground
    }
    if(!wasGroundedLastFrame && isGrounded) {
      landedTimer = 0.0f;
      PlayLanding();
    }
  }

  public void SetWeaponNode(Node3D node) {
    weaponNode = node;
    originalPos = weaponNode.Position;
    originalRot = weaponNode.Rotation;
    targetPos = originalPos;
    targetRot = originalRot;
    lastGlobalPos = weaponNode.GlobalPosition;
  }

  // ============================================================
  //  LOOK INERTIA
  // ============================================================
  private void UpdateLookInertia(float dt) {
    mouseVelTarget = new Vector2(
      Mathf.Clamp(-mouseVelocity.X * 0.00004f, -0.06f, 0.06f),
      Mathf.Clamp(-mouseVelocity.Y * 0.00004f, -0.06f, 0.06f)
    );
    mouseVelCurrent = mouseVelCurrent.Lerp(mouseVelTarget, lookInertiaDamping * dt);
    if(mouseVelCurrent.LengthSquared() < 1e-6f) { mouseVelCurrent = Vector2.Zero; }
  }

  // ============================================================
  //  BOB
  // ============================================================
  private void UpdateBob(float dt) {
    float targetBias = Mathf.Clamp(horizontalSpeed / 5.0f, 0.0f, 1.0f);
    bobTargetSmooth = Mathf.Lerp(bobTargetSmooth, targetBias, bobSmoothBlend * dt);
    bobT += dt * bobFrequency * bobTargetSmooth;
  }

  // ============================================================
  //  ACCELERATION SPRING
  // ============================================================
  private Vector3 accelSpringPos = Vector3.Zero;
  private Vector3 accelSpringVel = Vector3.Zero;
  private Vector3 lastMovementDir;
  private float lastHorizontalSpeed;

  private void UpdateAccelerationSpring(float dt) {
    if(weaponNode == null) { return; }

    float dy = horizontalSpeed - lastHorizontalSpeed;
    float impulse = dy * accelFeedbackScale;
    accelSpringVel += new Vector3(0.0f, impulse, -impulse * 0.5f) * dt * 30.0f;

    Vector3 force = -accelSpringStiffness * accelSpringPos - accelSpringDamping * accelSpringVel;
    accelSpringVel += force * dt;
    accelSpringPos += accelSpringVel * dt;
    accelSpringPos = accelSpringPos.Clamp(new Vector3(-accelClamp, -accelClamp, -accelClamp), new Vector3(accelClamp, accelClamp, accelClamp));

    lastHorizontalSpeed = horizontalSpeed;
  }

  // ============================================================
  //  LANDING
  // ============================================================
  private void UpdateLanding(float dt) {
    if(landedTimer > 0.0f) {
      landedTimer += dt;
      if(landedTimer > landDuration) { landedTimer = -1.0f; }
    }
  }

  private void PlayLanding() {
    if(weaponNode == null) { return; }
    StopAction();
    ResetToOriginal();
    actionTween = CreateTween();
    isActionPlaying = true;
    actionTween.Finished += () => isActionPlaying = false;

    Vector3 downPos = originalPos + new Vector3(0.0f, landDipY, 0.0f);

    actionTween.Parallel().TweenProperty(weaponNode, "position", downPos, landDuration * 0.35f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", originalRot + new Vector3(0.08f, 0.0f, 0.0f), landDuration * 0.35f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

    actionTween.TweenInterval(landDuration * 0.20f);
    actionTween.Parallel().TweenProperty(weaponNode, "position", originalPos, landDuration * 0.65f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", originalRot, landDuration * 0.65f)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
  }

  // ============================================================
  //  RECOIL SPRING (stackable)
  // ============================================================
  private void UpdateRecoilSpring(float dt) {
    if(isActionPlaying) { return; }

    Vector3 fPos = -recoilSpringStiffness * recoilPos - recoilSpringDamping * recoilVel;
    recoilVel += fPos * dt;
    recoilPos += recoilVel * dt;

    Vector3 fRot = -recoilSpringStiffness * recoilRot - recoilSpringDamping * recoilRotVel;
    recoilRotVel += fRot * dt;
    recoilRot += recoilRotVel * dt;
  }

  public void AddRecoilImpulse() {
    float sV = (float)GD.RandRange(1.0f - recoilVariation, 1.0f + recoilVariation);
    float side = (float)GD.RandRange(-recoilSideDriftMax, recoilSideDriftMax);

    recoilVel += new Vector3(side, 0.0f, -recoilKickZ * sV) * 24.0f;
    recoilRotVel += new Vector3(recoilKickPitch * sV, 0.0f, -side * 4.0f) * 20.0f;

    PlayMuzzleFlash();
    EmitSignalCameraShake(cameraShakeAmount, cameraShakeDuration);
    EmitSignalCameraRecoil(cameraShakeAmount * 1.2f);
  }

  private void PlayMuzzleFlash() {
    if(weaponNode == null) { return; }
    Vector3 globalPos = weaponNode.GlobalPosition;
    Vector3 forward = -weaponNode.GlobalTransform.Basis.Z;
    EmitSignalMuzzleFlash(globalPos, forward);

    if(muzzleFlash != null) {
      muzzleFlash.Visible = true;
      muzzleFlash.LightEnergy = 6.0f;
      Tween t = CreateTween();
      t.TweenProperty(muzzleFlash, "light_energy", 0.0f, 0.07f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
      t.TweenCallback(Callable.From(() => muzzleFlash.Visible = false));
    }
  }

  // ============================================================
  //  SMOOTH APPLY
  // ============================================================
  private void SmoothApply(float dt) {
    if(weaponNode == null) { return; }
    if(isActionPlaying) { return; }

    UpdateRecoilSpring(dt);

    Vector3 p = weaponNode.Position.Lerp(targetPos + accelSpringPos + recoilPos, positionSmoothSpeed * dt);
    Vector3 r = weaponNode.Rotation.Lerp(targetRot + recoilRot, rotationSmoothSpeed * dt);
    weaponNode.Position = p;
    weaponNode.Rotation = r;
  }

  // ============================================================
  //  RECOIL
  // ============================================================
  public void PlayRecoil() {
    if(weaponNode == null) { return; }
    AddRecoilImpulse();
  }

  // ============================================================
  //  RELOAD
  // ============================================================
  public void PlayReload(float totalDuration = 0.0f) {
    if(weaponNode == null) { return; }
    StopAction();
    ResetToOriginal();
    actionTween = CreateTween();
    isActionPlaying = true;

    float hold = reloadHoldDuration;
    if(totalDuration > 0.0f) {
      hold = Mathf.Max(0.0f, totalDuration - reloadDropDuration - reloadReturnDuration);
    }

    Vector3 dropPos = originalPos + new Vector3(reloadDropX, reloadDropY, reloadDropZ);
    Vector3 dropRot = originalRot + new Vector3(reloadRotX, reloadRotY, reloadRotZ);

    actionTween.Parallel().TweenProperty(weaponNode, "position", dropPos, reloadDropDuration)
      .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
    actionTween.Parallel().TweenProperty(weaponNode, "rotation", dropRot, reloadDropDuration)
      .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);

    actionTween.TweenInterval(hold);

    var returnTween = actionTween.Parallel();
    returnTween.TweenProperty(weaponNode, "position", originalPos, reloadReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
    returnTween.TweenProperty(weaponNode, "rotation", originalRot, reloadReturnDuration)
      .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

    // Use a reliable timer to ensure ammo refills exactly when animation ends
    Timer endTimer = new Timer();
    AddChild(endTimer);
    endTimer.OneShot = true;
    endTimer.Timeout += () => {
      isActionPlaying = false;
      EmitSignalReloadVisualComplete();
      endTimer.QueueFree();
    };
    endTimer.Start(reloadDropDuration + hold + reloadReturnDuration);
  }

  public void ForceFinishReload() {
    if(weaponNode == null) { return; }
    StopAction();
    ResetToOriginal();
    isActionPlaying = false;
    EmitSignalReloadVisualComplete();
  }

  // ============================================================
  //  SPRINT / JUMP / DASH
  // ============================================================
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

  // ============================================================
  //  HELPERS
  // ============================================================
  private void ResetToOriginal() {
    if(weaponNode == null) { return; }
    weaponNode.Position = originalPos;
    weaponNode.Rotation = originalRot;
    targetPos = originalPos;
    targetRot = originalRot;
    positionVel = Vector3.Zero;
    rotationVel = Vector3.Zero;
    accelSpringPos = Vector3.Zero;
    accelSpringVel = Vector3.Zero;
    bobT = 0.0f;
    bobTargetSmooth = 0.0f;
  }

  private void StopAction() {
    actionTween?.Kill();
    isActionPlaying = false;
  }

  public bool IsBusy() => isActionPlaying;
}
