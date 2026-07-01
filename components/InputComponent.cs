using Godot;

// Input actions are stored as booleans
public partial class InputPackage : Resource {
  // Movement
  public Vector2 direction = Vector2.Zero;

  // Actions
  public bool jump;
  public bool sprint;
  public bool dash;
  public bool shoot;
  public bool special;

  // Future actions
  public bool interact;
  public bool reload;
  public bool openInventory;
  public bool pause;
}

[GlobalClass]
public partial class InputComponent : Node {
  public InputPackage input = new();

  public InputPackage GetInput => input;

  public override void _UnhandledInput(InputEvent @event) {
    // Movement (WASD)
    input.direction = Input.GetVector(
      "move_left",
      "move_right",
      "move_forward",
      "move_backward"
    );

    // Actions
    input.jump = Input.IsActionJustPressed("jump");
    input.sprint = Input.IsActionPressed("sprint");
    input.dash = Input.IsActionJustPressed("dash");
    input.shoot = Input.IsActionJustPressed("shoot");
    input.special = Input.IsActionJustPressed("special");

    // Future actions
    input.interact = Input.IsActionJustPressed("interact");
    input.reload = Input.IsActionJustPressed("reload");
    input.openInventory = Input.IsActionJustPressed("inventory");
    input.pause = Input.IsActionJustPressed("pause");

    // Debug Output
    if(input.direction != Vector2.Zero) {
      GD.Print($"Movement: {input.direction}");
    }
    if(input.jump) { GD.Print("Action: Jump"); }
    if(input.sprint) { GD.Print("Action: Sprint"); }
    if(input.dash) { GD.Print("Action: Dash"); }
    if(input.shoot) { GD.Print("Action: Shoot"); }
    if(input.special) { GD.Print("Action: Special"); }
    if(input.interact) { GD.Print("Action: Interact"); }
    if(input.reload) { GD.Print("Action: Reload"); }
    if(input.openInventory) { GD.Print("Action: Open Inventory"); }
    if(input.pause) { GD.Print("Action: Pause"); }

    input.EmitChanged();
  }
}

