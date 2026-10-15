using System;
using RotMG.Common;
using RotMG.Game.Logic.Behaviors;
using RotMG.Utils;
using Xunit;

namespace RotMG.BehaviorTests;

[Collection("Game resources")]
public sealed class MovementBehaviorTests
{
    private const string HostId = "Elf Mage";
    // Center within the movement sub-tile cell so this suite does not depend
    // on static map collision data at the neighboring cell boundary.
    private const float HostX = 50.25f;
    private const float HostY = 50.25f;

    [Fact]
    public void WanderCycleCompletionIsPerHost()
    {
        var world = TestWorldFactory.Create();
        var wander = new Wander(0.2f);
        var first = world.AddEnemy(HostId, HostX, HostY);
        var second = world.AddEnemy(HostId, HostX + 4, HostY);
        wander.Enter(first);
        wander.Enter(second);

        ((WanderState)first.StateObject[wander.Id]).RemainingDistance = 0;
        ((WanderState)second.StateObject[wander.Id]).RemainingDistance = 10;

        Assert.True(wander.Tick(first));
        Assert.True(wander.IsCycleComplete(first));
        Assert.False(wander.IsCycleComplete(second));
    }

    [Fact]
    public void WanderMovesNormallyOnSegmentBoundary()
    {
        var world = TestWorldFactory.Create();
        var wander = new Wander(0.2f);
        var steady = world.AddEnemy(HostId, HostX, HostY);
        var boundary = world.AddEnemy(HostId, HostX + 4, HostY);
        wander.Enter(steady);
        wander.Enter(boundary);

        var steadyState = (WanderState)steady.StateObject[wander.Id];
        steadyState.Direction = new Position(1, 0);
        steadyState.RemainingDistance = 10;
        var boundaryState = (WanderState)boundary.StateObject[wander.Id];
        boundaryState.RemainingDistance = 0;

        Position steadyStart = steady.Position;
        Position boundaryStart = boundary.Position;
        Assert.True(wander.Tick(steady));
        Assert.True(wander.Tick(boundary));

        float expected = steady.GetSpeed(wander.Speed) * Settings.SecondsPerTick;
        Assert.InRange(steadyStart.Distance(steady.Position), expected - 0.001f, expected + 0.001f);
        Assert.InRange(boundaryStart.Distance(boundary.Position), expected - 0.001f, expected + 0.001f);
        Assert.True(wander.IsCycleComplete(boundary));
    }

    [Fact]
    public void PrioritizeRechecksFollowAfterWanderBoundaryWithoutPausingBoundaryMovement()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy(HostId, HostX, HostY);
        var follow = new Follow(0.4, acquireRange: 20, range: 2);
        var wander = new Wander(0.2f);
        var prioritize = new Prioritize(follow, wander);
        prioritize.Enter(host);

        // With no player, the fallback Wander becomes active and starts its leg.
        Assert.True(prioritize.Tick(host));
        // Its first call selects a new leg at the cycle boundary. A second call
        // makes that running leg the selected child before the player appears.
        Assert.True(prioritize.Tick(host));
        var wanderState = (WanderState)host.StateObject[wander.Id];
        wanderState.Direction = new Position(1, 0);
        wanderState.RemainingDistance = 0;
        host.Position = new Position(HostX, HostY);

        // A player appears during the leg. The completion tick still moves Wander.
        world.AddPlayerAt(new Position(HostX + 8, HostY));
        Position boundaryStart = host.Position;
        Assert.True(prioritize.Tick(host));
        float expected = host.GetSpeed(wander.Speed) * Settings.SecondsPerTick;
        Assert.InRange(boundaryStart.Distance(host.Position), expected - 0.001f, expected + 0.001f);
        Assert.True(wander.IsCycleComplete(host));

        // The higher-priority Follow is selected on the next tick, and Wander is not
        // run a second time on the boundary tick.
        float remainingAtBoundary = wanderState.RemainingDistance;
        Position beforeFollow = host.Position;
        Assert.True(prioritize.Tick(host));
        Assert.Equal(FollowPhase.Acquired, ((FollowState)host.StateObject[follow.Id]).Phase);
        Assert.NotEqual(beforeFollow, host.Position);
        Assert.Equal(remainingAtBoundary, wanderState.RemainingDistance);
    }

    [Fact]
    public void PlayerTargetedMovementDoesNotSelectHostWhenNoPlayerExists()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy(HostId, HostX, HostY);

        var follow = new Follow(0.4);
        follow.Enter(host);
        Assert.False(follow.Tick(host));
        Assert.Equal(FollowPhase.DontKnowWhere, ((FollowState)host.StateObject[follow.Id]).Phase);

        var charge = new Charge();
        charge.Enter(host);
        Assert.False(charge.Tick(host));
        Assert.Equal(0, ((ChargeState)host.StateObject[charge.Id]).Direction.X);
        Assert.Equal(0, ((ChargeState)host.StateObject[charge.Id]).Direction.Y);

        var stayBack = new StayBack(0.4);
        Assert.False(stayBack.Tick(host));

        var orbit = new Orbit(0.5, 3);
        orbit.Enter(host);
        Assert.False(orbit.Tick(host));

        // Swirl's unacquired retry timer must not be reset as if the host were a player.
        var swirl = new Swirl(0.5, 8);
        swirl.Enter(host);
        var swirlState = (SwirlState)host.StateObject[swirl.Id];
        swirlState.RemainingTime = 30000;
        Assert.True(swirl.Tick(host));
        Assert.Equal(29900, swirlState.RemainingTime);
        Assert.False(swirlState.Acquired);
    }

    [Fact]
    public void NamedEntityTargetStillWorksWithoutPlayers()
    {
        var world = TestWorldFactory.Create();
        var host = world.AddEnemy(HostId, HostX, HostY);
        world.AddEnemy("Elf Wizard", HostX + 4, HostY);

        var stayBack = new StayBack(0, distance: 10, entity: "Elf Wizard");
        Assert.True(stayBack.Tick(host));

        var orbit = new Orbit(0.5, 2, acquireRange: 10, target: "Elf Wizard");
        orbit.Enter(host);
        Assert.True(orbit.Tick(host));
    }
}
