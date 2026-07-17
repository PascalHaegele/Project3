using Godot;
using System.Collections.Generic;

// Enables dynamic modification of actor stats via item sockets
[GlobalClass]
public partial class SocketComponent : Node {
  // Track socketed pages: key = socket slot ID, value = PageData
  private readonly Dictionary<string, PageData> socketedPages = new();

  // Signal emitted whenever the socket configuration changes
  [Signal]
  public delegate void SocketChangedEventHandler();

  /// <summary>
  /// Socket a page into a specific slot. Only the effect matching the slot category is applied.
  /// </summary>
  /// <param name="page">The PageData to socket</param>
  /// <param name="socketCategory">"Weapon", "Armor", or "Skill"</param>
  public void SocketPage(PageData page, string socketCategory) {
    if (page == null) return;

    string slotId = $"{socketCategory}_{socketedPages.Count}";

    // Remove existing page in same slot if any
    if (socketedPages.ContainsKey(slotId)) {
      PageData existing = socketedPages[slotId];
      (string existingEffect, float existingValue) = existing.GetEffectForCategory(socketCategory);
      RemoveModifier(existingEffect, existingValue);
    }

    // Apply new page's effect for this category
    (string newEffect, float newValue) = page.GetEffectForCategory(socketCategory);
    AddModifier(newEffect, newValue);

    socketedPages[slotId] = page;
    GD.Print($"Socketed {page.PageName} into {socketCategory} slot. Effect: {newEffect} = {newValue}");
    _ = EmitSignal(SignalName.SocketChanged);
  }

  /// <summary>
  /// Remove a page from a specific slot and remove its active modifier.
  /// </summary>
  public void RemovePage(PageData page, string socketCategory) {
    if (page == null) return;

    // Find and remove from socketed pages
    string slotIdToRemove = null;
    foreach (var kvp in socketedPages) {
      if (kvp.Value == page && kvp.Key.StartsWith(socketCategory)) {
        slotIdToRemove = kvp.Key;
        break;
      }
    }

    if (slotIdToRemove != null) {
      (string effect, float value) = page.GetEffectForCategory(socketCategory);
      RemoveModifier(effect, value);
      socketedPages.Remove(slotIdToRemove);
      GD.Print($"Removed {page.PageName} from {socketCategory} slot.");
      _ = EmitSignal(SignalName.SocketChanged);
    }
  }

  /// <summary>
  /// Move a page from one socket type to another. Removes old effect, applies new effect.
  /// </summary>
  public void MovePage(PageData page, string oldCategory, string newCategory) {
    // Remove from old category
    RemovePage(page, oldCategory);

    // Socket into new category (assign to first available slot of new category)
    SocketPage(page, newCategory);
  }

  // Internal: add a modifier to the active pool
  private void AddModifier(string effectName, float value) {
    if (string.IsNullOrEmpty(effectName)) return;

    // Handle special cases that aren't simple additive modifiers
    if (IsSpecialEffect(effectName)) {
      HandleSpecialEffect(effectName, value, add: true);
    } else {
      // Standard additive modifiers
      if (_modifiers.ContainsKey(effectName)) {
        _modifiers[effectName] += value;
      } else {
        _modifiers.Add(effectName, value);
      }
    }

    GD.Print($"Socket Modifier Added: {effectName} = {GetModifier(effectName)}");
  }

  // Internal: remove a modifier from the active pool
  private void RemoveModifier(string effectName, float value) {
    if (string.IsNullOrEmpty(effectName)) return;

    if (IsSpecialEffect(effectName)) {
      HandleSpecialEffect(effectName, value, add: false);
    } else {
      if (_modifiers.ContainsKey(effectName)) {
        _modifiers[effectName] -= value;
        if (_modifiers[effectName] <= 0.0f) {
          _modifiers.Remove(effectName);
        }
      }
    }

    GD.Print($"Socket Modifier Removed: {effectName}");
  }

  // Check if this effect requires special handling (not just a flat number)
  private bool IsSpecialEffect(string effectName) {
    return effectName == "FrenziedSoul" ||
           effectName == "HomingProjectiles" ||
           effectName == "EchoStep" ||
           effectName == "BloodRitual";
  }

  // Handle special effects that need state tracking or conditional logic
  private void HandleSpecialEffect(string effectName, float value, bool add) {
    // For now, store these as flat modifiers that other systems can query
    // More complex behaviors (procs, timers, etc.) are queried via HasModifier and GetModifier
    // The actual gameplay logic is implemented in the relevant systems (Weapon, Health, etc.)
    if (add) {
      if (_modifiers.ContainsKey(effectName)) {
        _modifiers[effectName] += value;
      } else {
        _modifiers.Add(effectName, value);
      }
    } else {
      if (_modifiers.ContainsKey(effectName)) {
        _modifiers[effectName] -= value;
        if (_modifiers[effectName] <= 0.0f) {
          _modifiers.Remove(effectName);
        }
      }
    }
  }

  // Standard modifier storage (additive)
  private readonly Dictionary<string, float> _modifiers = new();

  /// <summary>
  /// Returns the value of a specific modifier; returns 0 if it does not exist.
  /// </summary>
  public float GetModifier(string effectName) =>
    _modifiers.TryGetValue(effectName, out float value) ? value : 0f;

  /// <summary>
  /// Returns all active modifiers as a copy.
  /// </summary>
  public Dictionary<string, float> GetAllModifiers() => new(_modifiers);

  /// <summary>
  /// Checks if a specific modifier is currently active.
  /// </summary>
  public bool HasModifier(string effectName) => _modifiers.ContainsKey(effectName);

  /// <summary>
  /// Prints a summary of all active modifiers to the console.
  /// </summary>
  public void PrintModifiers() {
    GD.Print("------ Active Socket Modifiers ------");
    if (_modifiers.Count == 0) {
      GD.Print("No active modifiers.");
      return;
    }
    foreach (KeyValuePair<string, float> modifier in _modifiers) {
      GD.Print($"{modifier.Key}: {modifier.Value}");
    }
  }

  /// <summary>
  /// Returns all currently socketed pages with their slot IDs.
  /// </summary>
  public Dictionary<string, PageData> GetAllSocketedPages() => new(socketedPages);

  /// <summary>
  /// Clears all sockets and modifiers. Use with caution.
  /// </summary>
  public void ClearAllSockets() {
    _modifiers.Clear();
    socketedPages.Clear();
    GD.Print("All sockets cleared.");
    _ = EmitSignal(SignalName.SocketChanged);
  }
}