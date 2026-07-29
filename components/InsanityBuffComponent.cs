using Godot;

/// <summary>
/// Handles the Right Click Insanity Buff ability.
/// Consumes 30 insanity to grant a 10-second combat buff:
/// +25% Weapon Damage, +20% Movement Speed, 2 HP/sec regen (max 20 HP total).
/// 20-second cooldown.
/// </summary>
[GlobalClass]
public partial class InsanityBuffComponent : Node {
  // =========================
  // Settings
  // =========================
  private const float INSANITY_COST = 150.0f;
  private const float BUFF_DURATION = 10.0f;
  private const float COOLDOWN_DURATION = 20.0f;
  private const float DAMAGE_MULTIPLIER = 1.25f;
  private const float SPEED_MULTIPLIER = 1.20f;
  private const float HEAL_RATE = 2.0f;          // HP per second
  private const float MAX_HEAL_AMOUNT = 20.0f;    // Max healing over duration

  // =========================
  // References
  // =========================
  private Player player;
  private InsanityComponent insanityComponent;
  private HealthComponent healthComponent;
  private VelocityComponent velocityComponent;

  // =========================
  // Runtime State
  // =========================
  private float buffTimer;
  private float cooldownTimer;
  private float healAccumulator;
  private float totalHealedThisBuff;
  private bool isBuffActive;
  private bool isOnCooldown;

  // Store original multiplier to restore on buff expiry
  private float originalSpeedMultiplier;

  // =========================
  // Visual Nodes
  // =========================
  private Node3D buffAura;
  private GpuParticles3D buffParticles;

  // =========================
  // Signals
  // =========================
  [Signal] public delegate void BuffActivatedEventHandler();
  [Signal] public delegate void BuffExpiredEventHandler();
  [Signal] public delegate void CooldownStartedEventHandler(float cooldownDuration);
  [Signal] public delegate void CooldownUpdatedEventHandler(float remaining);
  [Signal] public delegate void CooldownFinishedEventHandler();

  // =========================
  // Properties
  // =========================
  public bool IsBuffActive => isBuffActive;
  public bool IsOnCooldown => isOnCooldown;
  public float BuffTimeRemaining => isBuffActive ? buffTimer : 0f;
  public float CooldownTimeRemaining => isOnCooldown ? cooldownTimer : 0f;
  public bool CanActivate =>
    !isBuffActive && !isOnCooldown &&
    insanityComponent != null && insanityComponent.CurrentInsanity >= INSANITY_COST;

  // =========================

  public override void _Ready() {
    player = GetParent<Player>();
    insanityComponent = player.GetComponent<InsanityComponent>();
    healthComponent = player.GetComponent<HealthComponent>();
    velocityComponent = player.GetComponent<VelocityComponent>();

    // Defer visual setup to avoid adding children during parent construction
    Callable.From(SetupVisuals).CallDeferred();
  }

  private void SetupVisuals() {
    // Create a dark golden aura root
    buffAura = new Node3D();
    buffAura.Name = "InsanityBuffAura";
    player.AddChild(buffAura);

    // ========== SUBTLE OMNI LIGHT ==========
    OmniLight3D glowLight = new();
    glowLight.Name = "BuffGlowLight";
    glowLight.LightColor = new Color(0.85f, 0.65f, 0.0f); // Dark gold
    glowLight.LightEnergy = 1.5f;
    glowLight.LightIndirectEnergy = 0.5f;
    glowLight.OmniRange = 4.0f;
    glowLight.OmniAttenuation = 0.6f;
    glowLight.ShadowEnabled = false;
    glowLight.Visible = false;
    glowLight.Position = new Vector3(0f, 1.2f, 0f);
    buffAura.AddChild(glowLight);

    // ========== SUBTLE GLOW SPHERE ==========
    MeshInstance3D auraMesh = new();
    auraMesh.Name = "BuffAuraMesh";
    SphereMesh sphereMesh = new();
    sphereMesh.Radius = 0.9f;
    sphereMesh.Height = 2.0f;
    
    StandardMaterial3D auraMaterial = new();
    auraMaterial.AlbedoColor = new Color(0.85f, 0.65f, 0.0f, 0.12f);
    auraMaterial.EmissionEnabled = true;
    auraMaterial.Emission = new Color(0.85f, 0.65f, 0.0f);
    auraMaterial.EmissionEnergyMultiplier = 0.8f;
    auraMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
    sphereMesh.Material = auraMaterial;
    auraMesh.Mesh = sphereMesh;
    auraMesh.Position = new Vector3(0f, 0.9f, 0f);
    auraMesh.Visible = false;
    buffAura.AddChild(auraMesh);

    // ========== FLOATING PARTICLES ==========
    buffParticles = new GpuParticles3D();
    buffParticles.Name = "BuffParticles";
    buffParticles.Emitting = false;
    buffParticles.OneShot = false;
    buffParticles.Amount = 20;
    buffParticles.Lifetime = 2.0f;
    buffParticles.Preprocess = 0.5f;
    buffParticles.SpeedScale = 0.3f;
    buffParticles.Explosiveness = 0.1f;
    buffParticles.Randomness = 0.8f;
    buffParticles.FixedFps = 0;

    ParticleProcessMaterial particleMat = new();
    particleMat.Direction = Vector3.Up;
    particleMat.Spread = 180.0f;
    particleMat.Gravity = Vector3.Zero;
    particleMat.InitialVelocityMin = 0.3f;
    particleMat.InitialVelocityMax = 0.8f;
    particleMat.ScaleMin = 0.04f;
    particleMat.ScaleMax = 0.12f;
    particleMat.Color = new Color(0.85f, 0.65f, 0.0f, 0.6f);
    particleMat.LifetimeRandomness = 0.3f;
    particleMat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
    particleMat.EmissionSphereRadius = 0.8f;

    buffParticles.ProcessMaterial = particleMat;
    buffParticles.Position = new Vector3(0f, 0.9f, 0f);
    buffParticles.Visible = false;
    buffAura.AddChild(buffParticles);
  }

  public override void _Process(double delta) {
    float dt = (float)delta;

    if (isBuffActive) {
      UpdateBuff(dt);
    }

    if (isOnCooldown) {
      UpdateCooldown(dt);
    }
  }

  /// <summary>
  /// Attempt to activate the buff. Returns true if successful.
  /// </summary>
  public bool Activate() {
    if (!CanActivate) { return false; }

    // Consume insanity
    insanityComponent.RemoveInsanity(INSANITY_COST);

    // Start buff
    isBuffActive = true;
    buffTimer = BUFF_DURATION;
    healAccumulator = 0f;
    totalHealedThisBuff = 0f;

    // Apply movement speed boost by modifying the velocity multiplier
    if (player.velocityInfo != null) {
      originalSpeedMultiplier = player.velocityInfo.multiplier;
      player.velocityInfo.multiplier = originalSpeedMultiplier * SPEED_MULTIPLIER;
    }

    // Show visual feedback
    ShowVisuals(true);

    // Start cooldown
    isOnCooldown = true;
    cooldownTimer = COOLDOWN_DURATION;

    GD.Print($"[InsanityBuff] ACTIVATED! +25% Damage, +20% Speed, HP Regen for {BUFF_DURATION}s");
    EmitSignalBuffActivated();
    EmitSignalCooldownStarted(COOLDOWN_DURATION);

    return true;
  }

  private void UpdateBuff(float dt) {
    buffTimer -= dt;
    if (buffTimer <= 0f) {
      DeactivateBuff();
      return;
    }

    // Health regeneration: 2 HP per second, max 20 total per buff activation
    if (totalHealedThisBuff < MAX_HEAL_AMOUNT && healthComponent != null && healthComponent.IsAlive) {
      healAccumulator += HEAL_RATE * dt;
      if (healAccumulator >= 1.0f) {
        int healAmount = Mathf.FloorToInt(healAccumulator);
        healAccumulator -= healAmount;

        // Clamp to max heal and never exceed max health
        float effectiveHeal = Mathf.Min(healAmount, MAX_HEAL_AMOUNT - totalHealedThisBuff);
        if (effectiveHeal > 0f) {
          healthComponent.Heal(effectiveHeal);
          totalHealedThisBuff += effectiveHeal;
        }
      }
    }
  }

  private void DeactivateBuff() {
    isBuffActive = false;
    buffTimer = 0f;
    healAccumulator = 0f;

    // Restore original speed multiplier
    if (player.velocityInfo != null) {
      player.velocityInfo.multiplier = originalSpeedMultiplier;
    }

    ShowVisuals(false);

    GD.Print("[InsanityBuff] Expired.");
    EmitSignalBuffExpired();
  }

  private void UpdateCooldown(float dt) {
    cooldownTimer -= dt;
    EmitSignalCooldownUpdated(cooldownTimer);

    if (cooldownTimer <= 0f) {
      isOnCooldown = false;
      cooldownTimer = 0f;
      GD.Print("[InsanityBuff] Cooldown finished.");
      EmitSignalCooldownFinished();
    }
  }

  private void ShowVisuals(bool show) {
    if (buffAura == null) { return; }

    foreach (Node child in buffAura.GetChildren()) {
      if (child is Node3D node3d) {
        node3d.Visible = show;
      }
    }

    if (buffParticles != null) {
      buffParticles.Emitting = show;
      buffParticles.Visible = show;
    }
  }

  // Subtle pulse for a living effect
  private float visualTimer;
  public override void _PhysicsProcess(double delta) {
    if (!isBuffActive) { return; }
    
    visualTimer += (float)delta;
    float pulse = 0.85f + 0.15f * Mathf.Sin(visualTimer * 3.0f);
    
    foreach (Node child in buffAura.GetChildren()) {
      if (child is OmniLight3D light) {
        light.LightEnergy = 1.2f + 0.3f * Mathf.Sin(visualTimer * 3.0f);
      }
      if (child is MeshInstance3D mesh && mesh.Mesh is SphereMesh sphere) {
        if (sphere.Material is StandardMaterial3D mat) {
          mat.EmissionEnergyMultiplier = 0.6f + 0.2f * Mathf.Sin(visualTimer * 3.0f);
        }
      }
    }
  }

  /// <summary>
  /// Forcefully deactivates the buff (used on reset/restart).
  /// </summary>
  public void ForceDeactivate() {
    if (isBuffActive) {
      DeactivateBuff();
    }
    // Also reset cooldown state
    isOnCooldown = false;
    cooldownTimer = 0f;
    ShowVisuals(false);
  }

  /// <summary>
  /// Returns the damage multiplier for use by the weapon/projectile system.
  /// +25% weapon damage when buff is active.
  /// </summary>
  public float GetDamageMultiplier() {
    return isBuffActive ? DAMAGE_MULTIPLIER : 1.0f;
  }
}
