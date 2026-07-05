using Godot;
using System.Collections.Generic;

public partial class HitLog : RefCounted {
  private readonly List<Node> log = new();

  public void LogHit(Node node) => log.Add(node);
  public bool HasHit(Node node) => log.Contains(node);
}

