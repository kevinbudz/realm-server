using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;
using RotMG.Utils;

namespace RotMG.Game.Logic.Behaviors
{
    public class CopyDamageRecordState
    {
        public int SinceCopy;
        public Entity Target;
    }

    //Port of the reference CopyDamageRecordBehavior: once a second, copies the damage record of the nearest entity of
    //targetId within Range (the nearest is picked again when the tracked one is gone). The damage record is
    //Enemy.DamageStorage, so the copy applies only when both entities are Enemies.
    public class CopyDamageRecord : Behavior
    {
        public readonly string TargetId;
        public readonly float Range;

        private ushort? _targetType;
        private bool _targetResolved;

        public CopyDamageRecord(string target, double range = 40)
        {
            TargetId = target;
            Range = (float)range;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new CopyDamageRecordState();
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is CopyDamageRecordState state))
                return false;

            state.SinceCopy += Settings.MillisecondsPerTick;
            if (state.SinceCopy < 1000)
                return false;
            state.SinceCopy = 0;

            ushort? type = TargetType();
            if (type == null)
                return false;
            if (state.Target == null)
                state.Target = BehaviorHelpers.NearestEntity(host, Range, type);
            if (state.Target == null)
                return false;
            if (state.Target.Parent == null || state.Target.Dead)
            {
                state.Target = null;
                return false;
            }

            if (host is Enemy hostEnemy && state.Target is Enemy targetEnemy &&
                Total(hostEnemy.DamageStorage) != Total(targetEnemy.DamageStorage))
                hostEnemy.DamageStorage = new Dictionary<int, int>(targetEnemy.DamageStorage);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }

        private ushort? TargetType()
        {
            if (!_targetResolved)
            {
                _targetResolved = true;
                if (!string.IsNullOrWhiteSpace(TargetId) && BehaviorHelpers.TryGetObjType(TargetId.Trim(), out ushort type))
                    _targetType = type;
#if DEBUG
                else if (!string.IsNullOrWhiteSpace(TargetId))
                    Program.Print(PrintType.Error, $"CopyDamageRecord: unknown target <{TargetId}>.");
#endif
            }
            return _targetType;
        }

        private static int Total(Dictionary<int, int> record)
        {
            int total = 0;
            foreach (int damage in record.Values)
                total += damage;
            return total;
        }
    }
}
