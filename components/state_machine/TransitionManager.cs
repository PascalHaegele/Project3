using Godot;
using System;
using System.Collections.Generic;

public class Transition {
  public StringName from;
  public StringName to;
  public Func<bool> condition;
  public int priority;
  public float cooldown;
  public float lastTriggered;
  public StringName name = "";

  public Transition(
    StringName from,
    StringName to,
    Func<bool> condition,
    int priority = 0
  ) {
    this.from = from;
    this.to = to;
    this.condition = condition;
    this.priority = priority;

    name = $"{from} -> {to}";
  }

  public bool CanTrigger() {
    float currentTime = Time.GetTicksMsec() / 1000.0f;
    return currentTime - lastTriggered >= cooldown && condition();
  }

  public void Trigger() => lastTriggered = Time.GetTicksMsec() / 1000.0f;
}

[GlobalClass]
public partial class TransitionManager : RefCounted {
  public Dictionary<StringName, List<Transition>> transitions = new();

  public List<Transition> globalTransitions = new();

  public int maxHistory = 50;
  public List<Dictionary<string, object>> transitionHistory = new();

  public Dictionary<string, object> GetDebugInfo {
    get {
      int transitionCount = 0;
      foreach(StringName state in transitions.Keys) {
        transitionCount += transitions[state].Count;
      }

      return new() {
        ["total transitions"] = transitionCount,
        ["global transitions"] = globalTransitions.Count,
        ["states with transitions"] = transitions.Keys,
        ["recent history"] = GetRecentTransitions(),
      };
    }
  }

  public Transition AddTransition(
    StringName from,
    StringName to,
    Func<bool> condition,
    int priority = 0
  ) {
    Transition transition = new(from, to, condition, priority);
    if(!transitions.TryGetValue(from, out _)) { transitions[from] = new(); }
    transitions[from].Add(transition);

    transitions[from].Sort((a, b) => a.priority.CompareTo(b.priority));

    return transition;
  }

  public Transition AddGlobalTransition(
    StringName to,
    Func<bool> condition,
    int priority = 100
  ) {
    Transition transition = new("*", to, condition, priority);
    globalTransitions.Add(transition);

    globalTransitions.Sort((a, b) => a.priority.CompareTo(b.priority));

    return transition;
  }

  public Transition? GetAvailableTransition(string state) {
    foreach(Transition transition in globalTransitions) {
      if(transition.CanTrigger()) { return transition; }
    }

    if(transitions.ContainsKey(state)) {
      foreach(Transition transition in transitions[state]) {
        if(transition.CanTrigger()) { return transition; }
      }
    }

    return null;
  }

  public void RecordTransition(
    StringName from,
    StringName to,
    string reason = ""
  ) {
    Dictionary<string, object> record = new() {
      ["from"] = from,
      ["to"] = to,
      ["time"] = Time.GetTicksMsec() / 1000.0f,
      ["reason"] = reason,
    };

    if(transitionHistory.Count >= maxHistory) { transitionHistory.RemoveAt(0); }
    transitionHistory.Add(record);
  }

  public Dictionary<string, object>[] GetRecentTransitions(int count = 5) {
    return [.. transitionHistory[^count..]];
  }

  public virtual bool ValidateTransition(StringName from, StringName to) {
    return from != to;
  }

  public void RemoveTransitionFrom(StringName state) {
    _ = transitions.Remove(state);
  }

  public void RemoveTransitionTo(StringName state) {
    foreach(StringName fromState in transitions.Keys) {
      _ = transitions[fromState].RemoveAll((t) => t.to == state);
    }
  }
}

