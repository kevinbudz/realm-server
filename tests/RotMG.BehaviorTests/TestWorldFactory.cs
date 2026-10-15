using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RotMG.Common;
using RotMG.Game;
using RotMG.Game.Entities;
using RotMG.Game.Logic;
using RotMG.Utils;

namespace RotMG.BehaviorTests;

internal static class TestWorldFactory
{
    private static readonly object Gate = new();
    private static bool _initialized;

    public static TestWorld Create()
    {
        EnsureInitialized();
        ushort floor = Resources.Type2Tile.Values.First(t => !t.NoWalk).Type;
        return new TestWorld(new FlatMap(128, 128, floor));
    }

    private static void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_initialized)
                return;

            Settings.ResourceDirectory = Path.Combine(AppContext.BaseDirectory, "Resources");
            Settings.TicksPerSecond = 10;
            Settings.MillisecondsPerTick = 100;
            Settings.SecondsPerTick = 0.1f;
            Settings.DatabaseDirectory = Path.Combine(Path.GetTempPath(), "rotmg-behavior-tests");
            FieldInfo pendingWork = typeof(Program).GetField("PendingWork", BindingFlags.Static | BindingFlags.NonPublic);
            pendingWork.SetValue(null, new ConcurrentQueue<Work>());
            Resources.Init();
            Manager.Behaviors = LoadBehaviorDatabase();
            _initialized = true;
        }
    }

    private static BehaviorDb LoadBehaviorDatabase()
    {
        var db = new BehaviorDb();
        Assembly assembly = typeof(BehaviorDb).Assembly;
        foreach (Type type in assembly.GetTypes().Where(t => typeof(IBehaviorDatabase).IsAssignableFrom(t) && !t.IsInterface))
            ((IBehaviorDatabase)Activator.CreateInstance(type)).Init(db);
        return db;
    }

    public sealed class TestWorld : World
    {
        internal TestWorld(IGameMap map) : base(map, "Test", "Test", 0) { }

        public Enemy AddEnemy(string id, float x = 50.5f, float y = 50.5f)
        {
            ObjectDesc desc = Resources.Id2Object[id];
            var enemy = new Enemy(desc.Type);
            AddEntity(enemy, new Position(x, y));
            return enemy;
        }

        public Player AddPlayerAt(Position position)
        {
            var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
            player.Effects = new int[29];
            player.Parent = this;
            player.Position = position;
            player.Id = ++NextObjectId;
            PlayerChunks.Insert(player);
            return player;
        }

        public Player AddPlayerAt(Entity near, float distance = 1)
        {
            return AddPlayerAt(new Position(near.Position.X + distance, near.Position.Y));
        }
    }

    private sealed class FlatMap : IGameMap
    {
        public int Width { get; }
        public int Height { get; }
        public Dictionary<Region, List<IntPoint>> Regions { get; } = new();
        public bool HasPaintedTerrain => false;

        private readonly ushort _floor;
        public FlatMap(int width, int height, ushort floor) { Width = width; Height = height; _floor = floor; }
        public ushort GetGroundType(int x, int y) => _floor;
        public ushort GetObjectType(int x, int y) => 0xff;
        public Region GetRegion(int x, int y) => default;
        public string GetObjCfg(int x, int y) => string.Empty;
        public TerrainType GetTerrain(int x, int y) => default;
        public byte GetElevation(int x, int y) => 0;
    }
}
