using System;
using System.Collections.Generic;

public enum NodeState { SUCCESS, FAILURE, RUNNING }

public abstract class BehaviorTreeNode {
  protected NodeState state;
  public NodeState State => state;

  public abstract NodeState Evaluate();

  public virtual void Enter() { }

  public virtual void Exit() { }
}

public abstract class CompositeNode : BehaviorTreeNode {
  protected List<BehaviorTreeNode> children = new();

  public void AddChild(BehaviorTreeNode node) {
    children.Add(node);
  }

  public void AddChildren(params BehaviorTreeNode[] nodes) {
    foreach(BehaviorTreeNode node in nodes) {
      AddChild(node);
    }
  }
}

public class SelectorNode : CompositeNode {
  private int currentIndex;

  public override NodeState Evaluate() {
    foreach(BehaviorTreeNode child in children) {
      switch(child.Evaluate()) {
        case NodeState.SUCCESS:
          state = NodeState.SUCCESS;
          return state;
        case NodeState.RUNNING:
          state = NodeState.RUNNING;
          return state;
        case NodeState.FAILURE:
        default:
          continue;
      }
    }
    state = NodeState.FAILURE;
    return state;
  }
}

public class SequenceNode : CompositeNode {
  private int currentIndex;

  public override NodeState Evaluate() {
    foreach(BehaviorTreeNode child in children) {
      switch(child.Evaluate()) {
        case NodeState.FAILURE:
          state = NodeState.FAILURE;
          return state;
        case NodeState.RUNNING:
          state = NodeState.RUNNING;
          return state;
        case NodeState.SUCCESS:
        default:
          continue;
      }
    }

    state = NodeState.SUCCESS;
    return state;
  }
}

public class TaskNode : BehaviorTreeNode {
  private readonly Func<NodeState> task;

  public TaskNode(Func<NodeState> taskFunction) {
    task = taskFunction;
  }

  public override NodeState Evaluate() {
    state = task();
    return state;
  }
}

public class ConditionNode : BehaviorTreeNode {
  private readonly Func<bool> condition;

  public ConditionNode(Func<bool> conditionFunction) {
    condition = conditionFunction;
  }

  public override NodeState Evaluate() {
    state = condition() ? NodeState.SUCCESS : NodeState.FAILURE;
    return state;
  }
}

