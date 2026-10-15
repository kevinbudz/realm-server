using System.Linq;
using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Logic;
using Xunit;

namespace RotMG.BehaviorTests;

public sealed class DatabaseRegistrationTests
{
    private static readonly string[] PreviouslyMissingTargetEnemies =
    {
        "135-315 Diagonal Trap",
        "45-225 Diagonal Trap",
        "Adult White Dragon",
        "Bonegrind the Butcher",
        "Butcher",
        "Dragon Egg",
        "East Tutorial Gun",
        "Evil Chicken",
        "Evil Chicken God",
        "Evil Chicken Minion",
        "Evil Hen",
        "Flamer",
        "Flamer King",
        "Flayer",
        "Flayer God",
        "Flayer Veteran",
        "Great Coil Snake",
        "Great Snake Egg",
        "Great Temple Snake",
        "Juvenile White Dragon",
        "Kitchen Guard",
        "Left Horizontal Trap",
        "Lizard God",
        "Minotaur",
        "Night Elf Archer",
        "Night Elf King",
        "Night Elf Mage",
        "Night Elf Veteran",
        "Night Elf Warrior",
        "North Tutorial Gun",
        "Ogre King",
        "Ogre Mage",
        "Ogre Warrior",
        "Ogre Wizard",
        "Shield Orc Flooder",
        "Shield Orc Key",
        "Shield Orc Shield",
        "Soulless Dwarf",
        "South Tutorial Gun",
        "Test Egg",
        "TestChicken 2",
        "TestChicken Small",
        "Top Vertical Trap",
        "Undead Dwarf Axebearer",
        "Undead Dwarf God",
        "Undead Dwarf King",
        "Undead Dwarf Mage",
        "Undead Dwarf Warrior",
        "Urgle",
        "West Tutorial Gun",
        "White Dragon Whelp",
        "Zombie Trickster"
    };

    [Fact]
    public void AllTargetXmlSupportedReferenceEnemyGapsHaveBehaviorModels()
    {
        TestWorldFactory.Create();
        BehaviorDb db = Manager.Behaviors;

        Assert.Equal(52, PreviouslyMissingTargetEnemies.Length);
        Assert.Equal(586, db.Models.Count);

        string[] unresolved = PreviouslyMissingTargetEnemies
            .Where(id => !Resources.Id2Object.TryGetValue(id, out ObjectDesc desc) || !db.Models.ContainsKey(desc.Type))
            .ToArray();
        Assert.Empty(unresolved);
    }
}
