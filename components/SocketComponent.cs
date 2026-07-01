using Godot;
using System.Collections.Generic;

// Enables dynamic modification of weapon stats via item sockets
[GlobalClass]
public partial class SocketComponent : Node {
  // Stores all active modifiers (key: effect name, value: total magnitude)
  private readonly Dictionary<string, float> modifiers = new();

  // Signal emitted whenever the modifier list changes
  [Signal]
  public delegate void SocketChangedEventHandler();

  // Adds a new modifier or upgrades an existing one
  public void SocketPage(string effectName, float value) {
    if(modifiers.ContainsKey(effectName)) {
      modifiers[effectName] += value;
    } else {
      modifiers.Add(effectName, value);
    }

    GD.Print($"{effectName} = {modifiers[effectName]}");
    _ = EmitSignal(SignalName.SocketChanged);
  }

  // Removes a specific modifier and triggers an update
  public void RemovePage(string effectName) {
    if(modifiers.Remove(effectName)) {
      GD.Print($"{effectName} removed.");
      _ = EmitSignal(SignalName.SocketChanged);
    }
  }

  // Returns the value of a specific modifier; returns 0 if it does not exist
  public float GetModifier(string effectName) =>
    modifiers.TryGetValue(effectName, out float value) ? value : 0f;

  // Returns a copy of all active modifiers
  public Dictionary<string, float> GetAllModifiers() => new(modifiers);

  // Clears all active modifiers from the component
  public void ClearSockets() {
    modifiers.Clear();
    GD.Print("All sockets cleared.");
    _ = EmitSignal(SignalName.SocketChanged);
  }

  // Checks if a specific modifier is currently active
  public bool HasModifier(string effectName) => modifiers.ContainsKey(effectName);

  // Prints a summary of all active modifiers to the console
  public void PrintModifiers() {
    GD.Print("------ Active Socket Modifiers ------");

    if(modifiers.Count == 0) {
      GD.Print("No active modifiers.");
      return;
    }

    foreach(KeyValuePair<string, float> modifier in modifiers) {
      GD.Print($"{modifier.Key}: {modifier.Value}");
    }
  }
}

