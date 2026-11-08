using RotMG.Common;
using System;

namespace RotMG.Game.Logic.Behaviors
{
    //Hand-written toolkit: not emitted by Tools/XmlBehaviorConverter (files/xml has no equivalent).
    //Old ShootBehavior type="forward": fires along the host's movement direction. The direction is refreshed
    //every tick from the position delta since the previous tick and kept while the host stands still. It starts
    //at DefaultAngle (or 0). This overrides Shoot's fixed-angle hook, so the FixedAngle argument is ignored; all
    //other parameters forward to Shoot unchanged.
    public class ShootForward : Shoot
    {
        //Per-entity movement record. Stored under StateObject[-Id]: Shoot keys its own StateObject entry by Id
        //(RotateAngle only), and behavior ids are always positive, so the negative key cannot collide with it.
        private sealed class Track
        {
            public Position Last;
            public float Angle;
        }

        public ShootForward(
            float range = 5,
            byte count = 1,
            float? shootAngle = null,
            int index = 0,
            float? fixedAngle = null,
            float? rotateAngle = null,
            float angleOffset = 0,
            float? defaultAngle = null,
            float predictive = 0,
            int cooldownOffset = 0,
            int cooldownVariance = 0,
            int cooldown = 0)
            : base(range, count, shootAngle, index, fixedAngle, rotateAngle, angleOffset, defaultAngle,
                  predictive, cooldownOffset, cooldownVariance, cooldown)
        {
        }

        public override void Enter(Entity host)
        {
            base.Enter(host);
            host.StateObject[-Id] = NewTrack(host);
        }

        public override bool Tick(Entity host)
        {
            Track track = GetTrack(host);
            float dx = host.Position.X - track.Last.X;
            float dy = host.Position.Y - track.Last.Y;
            if (dx != 0 || dy != 0)
                track.Angle = (float)Math.Atan2(dy, dx);
            track.Last = host.Position;

            return base.Tick(host);
        }

        protected override float? GetFixedAngle(Entity host) => GetTrack(host).Angle;

        public override void Exit(Entity host)
        {
            base.Exit(host);
            host.StateObject.Remove(-Id);
        }

        private Track GetTrack(Entity host)
        {
            if (host.StateObject.TryGetValue(-Id, out object value) && value is Track track)
                return track;

            track = NewTrack(host);
            host.StateObject[-Id] = track;
            return track;
        }

        private Track NewTrack(Entity host)
        {
            return new Track { Last = host.Position, Angle = DefaultAngle ?? 0 };
        }
    }
}
