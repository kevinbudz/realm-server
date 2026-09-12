using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace RotMG.Common
{
    public static class Settings
    {
        public static int MaxClients;
        //Per-IP game-socket cap. World transfers briefly hold two sockets
        //for the same player, so a household of 4 behind one NAT needs
        //headroom above 4 (P18).
        public static int MaxClientsPerIp = 8;
        public static string Address;
        public static int[] Ports;
        public static string ResourceDirectory;
        public static string DatabaseDirectory;
        public static string DatabasePath;
        public static int TicksPerSecond;
        public static int MillisecondsPerTick;
        public static float SecondsPerTick;
        //Soak-test flag (default off): build large worlds (realm reset,
        //castle siege) on a worker thread and publish on the main thread
        //instead of stalling every tick for the full construction.
        public static bool AsyncWorldCreation;
        //Realm instance count for spreading players across worlds
        //(default 1 = legacy single realm). Each instance gets its own
        //Nexus portal, overseer lifecycle, and close/quake/reset cycle.
        public static int RealmInstances;
        //Bump together with realm-client Parameters.BUILD_VERSION whenever
        //PacketId, StatType, ConditionEffect bits or GameData change.
        public static string BuildVersion = "1.0.0";

        public static void Init()
        {
            if (File.Exists("Settings.xml"))
            {
                XElement data = XElement.Parse(File.ReadAllText("Settings.xml"));
                MaxClients = data.ParseInt("MaxClients", 256);
                MaxClientsPerIp = Math.Max(1, data.ParseInt("MaxClientsPerIp", 8));
                Address = data.ParseString("Address", "127.0.0.1");
                Ports = data.ParseIntArray("Ports", ":");
                ResourceDirectory = data.ParseString("@res", "Common/Resources");
                DatabaseDirectory = data.ParseString("@db", "Database");
                //The db attribute is either a directory (realm.db is created inside it)
                //or a direct path to a .db/.sqlite file.
                DatabasePath = DatabaseDirectory;
                if (DatabasePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                    DatabasePath.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                    DatabasePath.EndsWith(".sqlite3", StringComparison.OrdinalIgnoreCase))
                {
                    DatabaseDirectory = Path.GetDirectoryName(DatabasePath);
                    if (string.IsNullOrWhiteSpace(DatabaseDirectory))
                        DatabaseDirectory = ".";
                }
                else
                {
                    DatabasePath = Path.Combine(DatabaseDirectory, "realm.db");
                }
                TicksPerSecond = data.ParseInt("TicksPerSecond", 5);
                MillisecondsPerTick = 1000 / TicksPerSecond;
                SecondsPerTick = 1f / TicksPerSecond;
                AsyncWorldCreation = data.ParseBool("AsyncWorldCreation", false);
                RealmInstances = Math.Max(1, data.ParseInt("RealmInstances", 1));
                BuildVersion = data.ParseString("BuildVersion", "1.0.0");
            }
        }
    }
}
