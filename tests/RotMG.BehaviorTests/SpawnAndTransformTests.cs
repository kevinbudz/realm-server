using System.Linq;
using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Logic.Behaviors;
using Xunit;

namespace RotMG.BehaviorTests;

public sealed class SpawnAndTransformTests
{
    [Fact]
    public void FractionalInitialSpawnLeavesRoomForOneCappedChildAfterCooldown()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy("Minotaur");
        var spawn = new Spawn("Elf Mage", maxChildren: 2, initialSpawn: 0.5, cooldown: 500);
        spawn.Enter(host);

        ushort childType = Resources.Id2Object["Elf Mage"].Type;
        Assert.Equal(1, world.Entities.Values.Count(e => e.Type == childType));
        Assert.Equal(1, ((SpawnState)host.StateObject[spawn.Id]).CurrentNumber);

        for (int tick = 0; tick < 5; tick++)
            Assert.False(spawn.Tick(host));
        Assert.True(spawn.Tick(host));
        Assert.Equal(2, world.Entities.Values.Count(e => e.Type == childType));
        Assert.Equal(2, ((SpawnState)host.StateObject[spawn.Id]).CurrentNumber);

        for (int tick = 0; tick < 20; tick++)
            spawn.Tick(host);
        Assert.Equal(2, world.Entities.Values.Count(e => e.Type == childType));
    }

    [Fact]
    public void InitialSpawnAboveOneClampsToMaxChildrenAndNeverExceedsCap()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy("Minotaur");
        var spawn = new Spawn("Elf Mage", maxChildren: 2, initialSpawn: 2);
        spawn.Enter(host);

        ushort childType = Resources.Id2Object["Elf Mage"].Type;
        Assert.Equal(2, world.Entities.Values.Count(e => e.Type == childType));
        Assert.Equal(2, ((SpawnState)host.StateObject[spawn.Id]).CurrentNumber);
        for (int tick = 0; tick < 20; tick++)
            spawn.Tick(host);
        Assert.Equal(2, world.Entities.Values.Count(e => e.Type == childType));
    }

    [Fact]
    public void ThessalDeathCreatesOceanVentEvenWithoutDropperRegistration()
    {
        var world = TestWorldFactory.Create();
        var thessal = world.AddEnemy("Thessal the Mermaid Goddess");
        ushort ventType = Resources.Id2Object["Ocean Vent"].Type;
        Assert.False(Resources.Id2Object.ContainsKey("Thessal Dropper"));

        foreach (var behavior in thessal.Behavior.Behaviors)
            behavior.Death(thessal);

        Assert.Single(world.Entities.Values.Cast<Entity>().Concat(world.Statics.Values), e => e.Type == ventType);
    }
}
