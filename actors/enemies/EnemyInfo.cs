using Godot;

[Tool, GlobalClass]
public partial class EnemyInfo : Resource {
  [Export] public Vector3[] patrolPath;

  [Export] public Vector3 leashPoint;
  [Export] public float leashLength = 25.0f;

  [Export] public float attackRange = 3.0f;

  public bool HasPatrol => patrolPath != null && patrolPath.Length > 0;
}

