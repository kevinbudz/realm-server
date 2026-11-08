using System;
using System.Collections.Generic;
using RotMG.Common;
using RotMG.Game.Entities;

namespace RotMG.Game.Logic.Behaviors
{
    public class DeleteObjectState
    {
        public bool Done;
    }

    //Port of the reference DeleteObjectBehavior: once per state entry, removes every entity and static of the listed
    //types within Radius. Radius 0 or an empty list removes nothing. Statics go through World.RemoveEntity, which also
    //clears their tile.
    public class DeleteObject : Behavior
    {
        public readonly string ObjectIds;
        public readonly float Radius;

        private HashSet<ushort> _types;

        public DeleteObject(string objectIds, double radius = 0)
        {
            ObjectIds = objectIds;
            Radius = (float)radius;
        }

        public override void Enter(Entity host)
        {
            host.StateObject[Id] = new DeleteObjectState();
        }

        public override bool Tick(Entity host)
        {
            if (!(host.StateObject.TryGetValue(Id, out object o) && o is DeleteObjectState state))
                return false;
            if (state.Done)
                return false;
            state.Done = true;

            HashSet<ushort> types = Types();
            if (types.Count == 0 || Radius <= 0 || host.Parent == null)
                return false;

            List<Entity> victims = new List<Entity>();
            foreach (Entity en in BehaviorHelpers.NearbyEntities(host, Radius))
            {
                if (!(en is Player) && types.Contains(en.Type))
                    victims.Add(en);
            }
            foreach (Entity en in victims)
                host.Parent.RemoveEntity(en);
            return true;
        }

        public override void Exit(Entity host)
        {
            host.StateObject.Remove(Id);
        }

        private HashSet<ushort> Types()
        {
            if (_types != null)
                return _types;

            _types = new HashSet<ushort>();
            if (string.IsNullOrWhiteSpace(ObjectIds))
                return _types;
            foreach (string raw in ObjectIds.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0)
                    continue;
                if (BehaviorHelpers.TryGetObjType(name, out ushort type))
                    _types.Add(type);
                else
                {
#if DEBUG
                    Program.Print(PrintType.Error, $"DeleteObject: unknown object <{name}>.");
#endif
                }
            }
            return _types;
        }
    }
}
