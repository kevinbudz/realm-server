using System.Collections.Generic;
using System.Linq;
using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Logic;
using RotMG.Game.Logic.Behaviors;
using RotMG.Game.Logic.Transitions;
using Xunit;

namespace RotMG.BehaviorTests;

[Collection("Game resources")]
public sealed class EngineLifecycleTests
{
    [Fact]
    public void RootTransitionMovesEnemyIntoSiblingPhaseAndDoesNotReenterActivePhase()
    {
        TestWorldFactory.TestWorld world = TestWorldFactory.Create();
        ObjectDesc desc = Resources.Type2Object.Values.First(d => d.Enemy);
        var shot = new Shoot(range: 0, cooldownOffset: 500, cooldown: 500);
        var model = new BehaviorModel(
            new PlayerWithinTransition(7, "Chase"),
            new State("Idle"),
            new State("Chase", shot));
        Manager.Behaviors.Models[desc.Type] = model;

        var enemy = world.AddEnemy(desc.Id);
        world.AddPlayerAt(enemy, 2);

        enemy.TickStates();
        Assert.Equal("chase", enemy.CurrentStates[0].StringId);
        Assert.Equal(500, enemy.StateCooldown[shot.Id]);

        enemy.TickStates();
        Assert.Equal("chase", enemy.CurrentStates[0].StringId);
        Assert.Equal(400, enemy.StateCooldown[shot.Id]);
    }

    [Fact]
    public void NoPlayerWithinIgnoresHostAndNearbyEnemiesButFindsPlayer()
    {
        TestWorldFactory.TestWorld world = TestWorldFactory.Create();
        ObjectDesc desc = Resources.Type2Object.Values.First(d => d.Enemy);
        var host = world.AddEnemy(desc.Id);
        world.AddEnemy(desc.Id, host.Position.X + 1, host.Position.Y);
        var transition = new NoPlayerWithinTransition(7, "idle");

        Assert.True(transition.Tick(host));

        world.AddPlayerAt(host, 2);
        Assert.False(transition.Tick(host));
    }

    [Fact]
    public void RootTransitionCanReturnToIdleWhenPlayerLeaves()
    {
        TestWorldFactory.TestWorld world = TestWorldFactory.Create();
        ObjectDesc desc = Resources.Type2Object.Values.First(d => d.Enemy);
        var chased = new PlayerWithinTransition(7, "Chase");
        var lost = new NoPlayerWithinTransition(7, "Idle");
        Manager.Behaviors.Models[desc.Type] = new BehaviorModel(
            chased,
            lost,
            new State("Idle"),
            new State("Chase"));

        var enemy = world.AddEnemy(desc.Id);
        world.AddPlayerAt(enemy, 2);
        enemy.TickStates();
        Assert.Equal("chase", enemy.CurrentStates[0].StringId);

        // Removing the visible player makes the sibling root transition true.
        foreach (var chunk in world.PlayerChunks.Chunks)
            foreach (var entry in chunk.Entities.ToArray())
                chunk.Entities.Remove(entry);
        enemy.ConditionEffects = ConditionEffects.Berserk;
        enemy.TickStates();
        Assert.Equal("idle", enemy.CurrentStates[0].StringId);
    }
}

[CollectionDefinition("Game resources", DisableParallelization = true)]
public sealed class GameResourceCollection { }
