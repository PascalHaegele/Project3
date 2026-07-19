using Godot;
using System.Collections.Generic;

/// <summary>
/// Static database of all available Pages in the game.
/// Contains exactly 9 effects: 3 Weapon, 3 Armor, 3 Skill.
/// </summary>
public static class PageDatabase {
  private static readonly Dictionary<string, PageData> _pages = new();

  static PageDatabase() {
    RegisterPages();
  }

  private static void RegisterPages() {
    // Page A: Damage + Movement Speed + Blood Ritual
    AddPage(new PageData {
      PageName = "Page Alpha",
      Description = "A page of combat enhancements.",
      WeaponEffect = "Damage",
      WeaponEffectValue = 10f,
      ArmorEffect = "MovementSpeed",
      ArmorEffectValue = 15f,
      SkillEffect = "BloodRitual",
      SkillEffectValue = 5f,
      Rarity = "Common"
    });

    // Page B: Reload Speed + Damage Reduction + Echo Step
    AddPage(new PageData {
      PageName = "Page Beta",
      Description = "A page of defensive and utility tricks.",
      WeaponEffect = "ReloadSpeed",
      WeaponEffectValue = 15f,
      ArmorEffect = "DamageReduction",
      ArmorEffectValue = 5f,
      SkillEffect = "EchoStep",
      SkillEffectValue = 2f,
      Rarity = "Common"
    });

    // Page C: Homing Projectiles + Dash Distance + Frenzied Soul
    AddPage(new PageData {
      PageName = "Page Gamma",
      Description = "A page of aggressive tactics.",
      WeaponEffect = "HomingProjectiles",
      WeaponEffectValue = 1f,
      ArmorEffect = "DashDistance",
      ArmorEffectValue = 15f,
      SkillEffect = "FrenziedSoul",
      SkillEffectValue = 10f,
      Rarity = "Common"
    });

    // More pages with different combinations
    AddPage(new PageData {
      PageName = "Page Delta",
      Description = "Balance in all things.",
      WeaponEffect = "Damage",
      WeaponEffectValue = 5f,
      ArmorEffect = "DamageReduction",
      ArmorEffectValue = 5f,
      SkillEffect = "BloodRitual",
      SkillEffectValue = 5f,
      Rarity = "Common"
    });

    AddPage(new PageData {
      PageName = "Page Epsilon",
      Description = "Speed and precision.",
      WeaponEffect = "ReloadSpeed",
      WeaponEffectValue = 10f,
      ArmorEffect = "MovementSpeed",
      ArmorEffectValue = 10f,
      SkillEffect = "FrenziedSoul",
      SkillEffectValue = 10f,
      Rarity = "Common"
    });

    AddPage(new PageData {
      PageName = "Page Zeta",
      Description = "Evasion and tricks.",
      WeaponEffect = "HomingProjectiles",
      WeaponEffectValue = 1f,
      ArmorEffect = "DashDistance",
      ArmorEffectValue = 10f,
      SkillEffect = "EchoStep",
      SkillEffectValue = 2f,
      Rarity = "Common"
    });

    AddPage(new PageData {
      PageName = "Page Eta",
      Description = "Pure offensive power.",
      WeaponEffect = "Damage",
      WeaponEffectValue = 15f,
      ArmorEffect = "MovementSpeed",
      ArmorEffectValue = 10f,
      SkillEffect = "FrenziedSoul",
      SkillEffectValue = 10f,
      Rarity = "Uncommon"
    });

    AddPage(new PageData {
      PageName = "Page Theta",
      Description = "Survivability toolkit.",
      WeaponEffect = "ReloadSpeed",
      WeaponEffectValue = 10f,
      ArmorEffect = "DamageReduction",
      ArmorEffectValue = 10f,
      SkillEffect = "BloodRitual",
      SkillEffectValue = 5f,
      Rarity = "Uncommon"
    });

    AddPage(new PageData {
      PageName = "Page Iota",
      Description = "Tactical versatility.",
      WeaponEffect = "HomingProjectiles",
      WeaponEffectValue = 1f,
      ArmorEffect = "DamageReduction",
      ArmorEffectValue = 5f,
      SkillEffect = "EchoStep",
      SkillEffectValue = 2f,
      Rarity = "Uncommon"
    });

    AddPage(new PageData {
      PageName = "Page Kappa",
      Description = "Hit and run tactics.",
      WeaponEffect = "Damage",
      WeaponEffectValue = 10f,
      ArmorEffect = "DashDistance",
      ArmorEffectValue = 15f,
      SkillEffect = "FrenziedSoul",
      SkillEffectValue = 10f,
      Rarity = "Uncommon"
    });

    AddPage(new PageData {
      PageName = "Page Lambda",
      Description = "Unstoppable force.",
      WeaponEffect = "Damage",
      WeaponEffectValue = 20f,
      ArmorEffect = "MovementSpeed",
      ArmorEffectValue = 15f,
      SkillEffect = "BloodRitual",
      SkillEffectValue = 10f,
      Rarity = "Rare"
    });

    AddPage(new PageData {
      PageName = "Page Mu",
      Description = "Evasive warfare.",
      WeaponEffect = "ReloadSpeed",
      WeaponEffectValue = 15f,
      ArmorEffect = "DashDistance",
      ArmorEffectValue = 20f,
      SkillEffect = "EchoStep",
      SkillEffectValue = 2f,
      Rarity = "Rare"
    });

    AddPage(new PageData {
      PageName = "Page Nu",
      Description = "Hunting ground.",
      WeaponEffect = "HomingProjectiles",
      WeaponEffectValue = 1f,
      ArmorEffect = "DamageReduction",
      ArmorEffectValue = 10f,
      SkillEffect = "FrenziedSoul",
      SkillEffectValue = 10f,
      Rarity = "Rare"
    });

    AddPage(new PageData {
      PageName = "Page Xi",
      Description = "Vampiric combat.",
      WeaponEffect = "Damage",
      WeaponEffectValue = 15f,
      ArmorEffect = "DamageReduction",
      ArmorEffectValue = 10f,
      SkillEffect = "BloodRitual",
      SkillEffectValue = 10f,
      Rarity = "Rare"
    });

    AddPage(new PageData {
      PageName = "Page Omicron",
      Description = "Speed demon.",
      WeaponEffect = "ReloadSpeed",
      WeaponEffectValue = 20f,
      ArmorEffect = "MovementSpeed",
      ArmorEffectValue = 20f,
      SkillEffect = "EchoStep",
      SkillEffectValue = 2f,
      Rarity = "Rare"
    });

    AddPage(new PageData {
      PageName = "Page Pi",
      Description = "Death incarnate.",
      WeaponEffect = "HomingProjectiles",
      WeaponEffectValue = 1f,
      ArmorEffect = "DashDistance",
      ArmorEffectValue = 20f,
      SkillEffect = "FrenziedSoul",
      SkillEffectValue = 10f,
      Rarity = "Rare"
    });
  }

  private static void AddPage(PageData page) {
    if (!_pages.ContainsKey(page.PageName)) {
      _pages.Add(page.PageName, page);
    }
  }

  public static PageData GetPage(string pageName) {
    _pages.TryGetValue(pageName, out PageData page);
    return page;
  }

  public static List<PageData> GetPagesByCategory(string category) {
    List<PageData> result = new();
    foreach (PageData page in _pages.Values) {
      result.Add(page);
    }
    return result;
  }

  public static List<string> GetAllPageNames() {
    return new List<string>(_pages.Keys);
  }

  public static int PageCount => _pages.Count;
}