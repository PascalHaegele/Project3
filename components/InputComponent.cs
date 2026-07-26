using Godot;

// Input actions are stored as booleans
public partial class InputPackage : Resource {
  public Vector2 direction = Vector2.Zero;

  public bool jump;
  public bool sprint;
  public bool dash;

  public bool shoot;
  public bool special;

  public bool interact;
  public bool openInventory;
  public bool usePotion;

  public bool pause;

  public bool reload;
  public bool weapon1;
  public bool weapon2;
  public bool switchWeapon;
}

[GlobalClass]
public partial class InputComponent : Node {
  public InputPackage GetInput() {
    InputPackage input = new();

    input.direction = Input.GetVector(
      "move_left",
      "move_right",
      "move_forward",
      "move_backward"
    );

    input.jump = Input.IsActionJustPressed("jump");
    input.sprint = Input.IsActionPressed("sprint");
    input.dash = Input.IsActionJustPressed("dash");

    input.shoot = Input.IsActionJustPressed("shoot");
    input.special = Input.IsActionJustPressed("special");

    input.interact = Input.IsActionJustPressed("interact");
    input.openInventory = Input.IsActionJustPressed("inventory");

    input.usePotion = Input.IsActionJustPressed("use_potion");

    input.pause = Input.IsActionJustPressed("pause");

    input.reload = Input.IsActionJustPressed("reload");
    input.weapon1 = Input.IsActionJustPressed("weapon1");
    input.weapon2 = Input.IsActionJustPressed("weapon2");
    input.switchWeapon = Input.IsActionJustPressed("switch_weapon");

    // Debug Output
    // if(input.direction != Vector2.Zero) {
    //   GD.Print($"Movement: {input.direction}");
    // }
    // if(input.jump) { GD.Print("Action: Jump"); }
    // if(input.sprint) { GD.Print("Action: Sprint"); }
    // if(input.dash) { GD.Print("Action: Dash"); }
    // if(input.shoot) { GD.Print("Action: Shoot"); }
    // if(input.special) { GD.Print("Action: Special"); }
    // if(input.interact) { GD.Print("Action: Interact"); }
    // if(input.reload) { GD.Print("Action: Reload"); }
    // if(input.openInventory) { GD.Print("Action: Open Inventory"); }
    // if(input.pause) { GD.Print("Action: Pause"); }

    input.EmitChanged();

    return input;
  }
}

