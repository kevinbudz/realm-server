using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Logic.Behaviors;
using Xunit;

namespace RotMG.BehaviorTests;

public sealed class CombatBehaviorTests
{
    private const string HostId = "Elf Mage";
    private const float HostX = 50.5f;
    private const float HostY = 50.5f;

    [Fact]
    public void ShootResolves500MillisecondCooldownOnSixth100MillisecondEvaluation()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy(HostId, HostX, HostY);
        host.StateCooldown = new Dictionary<int, int>();
        var shoot = new Shoot(range: 0, cooldown: 500);
        shoot.Enter(host);

        Assert.True(shoot.Tick(host)); // Initially due: consume no target and start cooldown.
        Assert.Equal(500, host.StateCooldown[shoot.Id]);

        for (int evaluation = 1; evaluation <= 5; evaluation++)
        {
            Assert.False(shoot.Tick(host));
            Assert.Equal(500 - evaluation * 100, host.StateCooldown[shoot.Id]);
        }

        Assert.True(shoot.Tick(host));
        Assert.Equal(500, host.StateCooldown[shoot.Id]);
    }

    [Fact]
    public void GrenadeUsesSameCooldownBoundaryAsShoot()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy(HostId, HostX, HostY);
        host.StateCooldown = new Dictionary<int, int>();
        var grenade = new Grenade(range: 0, cooldown: 500);
        grenade.Enter(host);

        Assert.True(grenade.Tick(host));
        Assert.Equal(500, host.StateCooldown[grenade.Id]);

        for (int evaluation = 1; evaluation <= 5; evaluation++)
        {
            Assert.False(grenade.Tick(host));
            Assert.Equal(500 - evaluation * 100, host.StateCooldown[grenade.Id]);
        }

        Assert.True(grenade.Tick(host));
        Assert.Equal(500, host.StateCooldown[grenade.Id]);
    }

    [Fact]
    public void ShootHonorsCooldownOffsetAndStunWithoutConsumingReadyShot()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy(HostId, HostX, HostY);
        host.StateCooldown = new Dictionary<int, int>();
        var shoot = new Shoot(range: 0, fixedAngle: 0, cooldownOffset: 300, cooldown: 500);
        shoot.Enter(host);
        shoot.Exit(host);
        shoot.Enter(host);
        Assert.Equal(300, host.StateCooldown[shoot.Id]);

        Assert.False(shoot.Tick(host));
        Assert.Equal(200, host.StateCooldown[shoot.Id]);
        Assert.False(shoot.Tick(host));
        Assert.Equal(100, host.StateCooldown[shoot.Id]);
        Assert.False(shoot.Tick(host));
        Assert.Equal(0, host.StateCooldown[shoot.Id]);
        Assert.True(shoot.Tick(host));
        Assert.Equal(500, host.StateCooldown[shoot.Id]);

        var stunnedHost = world.AddEnemy(HostId, HostX + 4, HostY);
        stunnedHost.StateCooldown = new Dictionary<int, int>();
        var stunnedShoot = new Shoot(range: 0, fixedAngle: 0, cooldown: 500);
        stunnedShoot.Enter(stunnedHost);
        stunnedHost.Effects[(int)ConditionEffectIndex.Stunned] = 1;
        Assert.False(stunnedShoot.Tick(stunnedHost));
        Assert.Equal(0, stunnedHost.StateCooldown[stunnedShoot.Id]);
        stunnedHost.Effects[(int)ConditionEffectIndex.Stunned] = 0;
        Assert.True(stunnedShoot.Tick(stunnedHost));

        Assert.Equal(2, world.NextProjectileId);
    }

    [Fact]
    public void DazedKeepsCeilingHalfProjectileCount()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy(HostId, HostX, HostY);
        host.StateCooldown = new Dictionary<int, int>();
        var shoot = new Shoot(range: 0, count: 5, fixedAngle: 0);
        shoot.Enter(host);
        host.Effects[(int)ConditionEffectIndex.Dazed] = 1;

        Assert.True(shoot.Tick(host));
        Assert.Equal(3, world.NextProjectileId);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 1)]
    public void PredictiveStrengthScalesLeadDeterministically(double strength, double expectedLead)
    {
        double halfPi = Math.PI / 2;
        double lead = PredictableShoot.Lead(
            new Position(0, 0),
            new Position(1, 0),
            new Position(0, 1),
            projectileSpeed: 100,
            strength,
            sampleMilliseconds: 100);

        Assert.Equal(expectedLead * halfPi / 0.1, lead, 8);
    }

    [Fact]
    public void PredictiveLeadUsesActualSampleIntervalAndStaysFiniteAcrossAngleWrap()
    {
        Position origin = new(0, 0);
        Position historical = new((float)Math.Cos(Math.PI - 0.05), (float)Math.Sin(Math.PI - 0.05));
        Position current = new((float)Math.Cos(-Math.PI + 0.05), (float)Math.Sin(-Math.PI + 0.05));

        double leadAt100Ms = PredictableShoot.Lead(origin, historical, current, 100, 1, 100);
        double leadAt200Ms = PredictableShoot.Lead(origin, historical, current, 100, 1, 200);
        double stationaryLead = PredictableShoot.Lead(origin, current, current, 100, 1, 100);

        Assert.True(double.IsFinite(leadAt100Ms));
        Assert.True(double.IsFinite(leadAt200Ms));
        Assert.InRange(leadAt100Ms, 0.99, 1.01);
        Assert.InRange(leadAt200Ms, 0.49, 0.51);
        Assert.Equal(0, stationaryLead, 8);
    }

    private sealed class PredictableShoot : Shoot
    {
        public static double Lead(
            Position origin,
            Position historicalTarget,
            Position currentTarget,
            float projectileSpeed,
            double strength,
            int sampleMilliseconds)
        {
            return Predict(origin, historicalTarget, currentTarget, projectileSpeed, strength, sampleMilliseconds);
        }
    }
}
