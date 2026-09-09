//Ported from realm-src-master's DungeonGen (RotMG Dungeon Generator,
//Copyright (C) 2015 creepylava, GNU AGPL v3). Generation logic matches the
//reference; only namespaces and hosting (resource/JSON/zlib) APIs differ.
using System;
using System.IO;
using RotMG.Game.DungeonGen.Dungeon;

namespace RotMG.Game.DungeonGen.Templates
{
    public abstract class DungeonTemplate
    {
        protected Random Rand { get; private set; }

        internal void SetRandom(Random rand)
        {
            Rand = rand;
        }

        public abstract int MaxDepth { get; }
        public abstract NormDist TargetDepth { get; }
        public virtual Range NumRoomRate { get { return new Range(3, 5); } }

        public abstract NormDist SpecialRmCount { get; }
        public abstract NormDist SpecialRmDepthDist { get; }

        public abstract int CorridorWidth { get; }
        public abstract Range RoomSeparation { get; }

        public virtual void Initialize()
        {
        }

        public abstract Room CreateStart(int depth);
        public abstract Room CreateTarget(int depth, Room prev);
        public abstract Room CreateSpecial(int depth, Room prev);
        public abstract Room CreateNormal(int depth, Room prev);

        public virtual void InitializeRasterization(DungeonGraph graph)
        {
        }

        public virtual MapRender CreateBackground()
        {
            return new MapRender();
        }

        public virtual MapRender CreateOverlay()
        {
            return new MapRender();
        }

        public virtual MapCorridor CreateCorridor()
        {
            return new MapCorridor();
        }

        protected static DungeonTile[,] ReadTemplate(Type templateType)
        {
            string templateName = templateType.Namespace + ".template.jm";
            using (Stream stream = templateType.Assembly.GetManifestResourceStream(templateName))
            {
                if (stream == null)
                    throw new InvalidOperationException("Embedded dungeon template not found: " + templateName);
                using (StreamReader reader = new StreamReader(stream))
                    return JsonMap.Load(reader.ReadToEnd());
            }
        }
    }
}
