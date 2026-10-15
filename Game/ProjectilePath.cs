using RotMG.Common;
using System;

namespace RotMG.Game
{
    //Closed-form projectile position: the server twin of the client's
    //projectile_position in realm-cpp (src/objects/ProjectileProperties.cpp).
    //A bullet's position is a pure function of (desc, start, angle, bullet
    //id, elapsed ms), so the hit check evaluates it directly instead of
    //re-simulating the path. The formulas, operand order and phase rules
    //match the client line for line, and all math is double like the
    //client's. Keep this file literal: a "cleaner" rewrite can drift by
    //tiles on long Wavy/Boomerang paths.
    public static class ProjectilePath
    {
        //Client-sided bullet ids are negative and count down. The client's
        //`id % 2 ? 1 : -1` is a truthiness test, so a negative odd id counts
        //as odd (remainder -1). A C# `== 1` test flips the x sign of the
        //whole parametric offset for every odd negative id (wrong by up to
        //2 * Magnitude tiles), so use `!= 0` here. The y rule `id % 4 < 2`
        //is already sign-correct under C remainder.
        public static void Evaluate(ProjectileDesc desc, double startX, double startY, double angle, int bulletId, double elapsedMs, out double x, out double y)
        {
            double lifetime = desc.LifetimeMS;
            //The client parses Speed with integer() (Catalog.cpp), which
            //truncates: Speed 0.02 is a bullet that never moves. The server's
            //float Speed must be truncated the same way or the two disagree.
            double baseSpeed = (int)desc.Speed;
            double speed = baseSpeed;
            if (desc.Accelerate)
                speed *= elapsedMs / lifetime;
            if (desc.Decelerate)
                speed *= 2 - elapsedMs / lifetime;
            double distance = elapsedMs * (speed / 10000);
            double phase = bulletId % 2 == 0 ? 0 : Math.PI;

            x = startX;
            y = startY;
            if (desc.Wavy)
            {
                double theta = angle + Math.PI / 64 * Math.Sin(phase + 6 * Math.PI * elapsedMs / 1000);
                x += distance * Math.Cos(theta);
                y += distance * Math.Sin(theta);
            }
            else if (desc.Parametric)
            {
                double t = elapsedMs / lifetime * 2 * Math.PI;
                double sx = Math.Sin(t) * (bulletId % 2 != 0 ? 1 : -1);
                double sy = Math.Sin(2 * t) * (bulletId % 4 < 2 ? 1 : -1);
                double sin = Math.Sin(angle);
                double cos = Math.Cos(angle);
                x += (sx * cos - sy * sin) * desc.Magnitude;
                y += (sx * sin + sy * cos) * desc.Magnitude;
            }
            else
            {
                //Mirrors Projectile.as: the apex uses the base speed, not
                //the accelerated one.
                if (desc.Boomerang)
                {
                    double halfway = lifetime * (baseSpeed / 10000) / 2;
                    if (distance > halfway)
                        distance = halfway - (distance - halfway);
                }
                x += distance * Math.Cos(angle);
                y += distance * Math.Sin(angle);
                if (desc.Amplitude != 0)
                {
                    double deflection = desc.Amplitude * Math.Sin(phase + elapsedMs / lifetime * desc.Frequency * 2 * Math.PI);
                    x += deflection * Math.Cos(angle + Math.PI / 2);
                    y += deflection * Math.Sin(angle + Math.PI / 2);
                }
            }
        }

        public static Position PositionAt(ProjectileDesc desc, Position start, double angle, int bulletId, double elapsedMs)
        {
            Evaluate(desc, start.X, start.Y, angle, bulletId, elapsedMs, out double x, out double y);
            return new Position((float)x, (float)y);
        }
    }
}
