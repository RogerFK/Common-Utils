
using EXILED;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MEC;
using Scp914;
using Harmony;

/// <summary>
/// Thank you too everyone who contributed to this plugin, ily joker <3
/// </summary>

namespace Common_Utils
{
    public class Plugin : EXILED.Plugin
    {
        public static Plugin Instance { private set; get; }
        public override string getName => "Common-Utils";

        // classes be like mega stupid amirite ladies?

        public partial class Scp914ItemUpgrade
        {
            public ItemType ToUpgrade { get; set; }
            public ItemType UpgradedTo { get; set; }

            public static Scp914ItemUpgrade ParseString(string config)
            {
                string[] splitted = config.Split('-');
                DebugBoi("Adding upgrade: " + splitted[0] + " --> " + splitted[1]);
                return new Scp914ItemUpgrade() { ToUpgrade = (ItemType)Enum.Parse(typeof(ItemType), splitted[0], true), UpgradedTo = (ItemType)Enum.Parse(typeof(ItemType), splitted[1], true) };
            }
        }

        public partial class Scp914PlayerUpgrade
        {
            public RoleType ToUpgrade { get; set; }
            public RoleType UpgradedTo { get; set; }

            public static Scp914PlayerUpgrade ParseString(string config)
            {
                string[] splitted = config.Split('-');
                DebugBoi("Adding upgrade: " + splitted[0] + " --> " + splitted[1]);
                return new Scp914PlayerUpgrade() { ToUpgrade = (RoleType)Enum.Parse(typeof(RoleType), splitted[0], true), UpgradedTo = (RoleType)Enum.Parse(typeof(RoleType), splitted[1], true) };
            }
        }

        // Iven tory. lol

        public partial class CustomInventory
        {
            public List<ItemType> NtfCadet = null;

            public List<ItemType> NtfLieutenant = null;

            public List<ItemType> NtfCommander = null;

            public List<ItemType> ClassD = null;

            public List<ItemType> Scientist = null;

            public List<ItemType> NtfScientist = null;

            public List<ItemType> Chaos = null;

            public List<ItemType> Guard = null;

            public static List<ItemType> ConvertToItemList(List<string> list)
            {
                if (list == null)
                    return null;
                List<ItemType> listd = new List<ItemType>();
                foreach (string s in list)
                {
                    DebugBoi("Adding item " + s);
                    listd.Add((ItemType)Enum.Parse(typeof(ItemType), s, true));
                }
                return listd;
            }

        }



        public CoroutineHandle cor;

        // Config settings.

        public CustomInventory Inventories = new CustomInventory();

        public Dictionary<RoleType, int> roleHealth = new Dictionary<RoleType, int>();

        public Dictionary<Scp914PlayerUpgrade, Scp914Knob> scp914Roles = new Dictionary<Scp914PlayerUpgrade, Scp914Knob>();

        public Dictionary<Scp914ItemUpgrade, Scp914Knob> scp914Items = new Dictionary<Scp914ItemUpgrade, Scp914Knob>();

        public EventHandlers EventHandler;

        public HarmonyInstance HarmonyInstance { private set; get; }
        
        public static List<CoroutineHandle> Coroutines = new List<CoroutineHandle>();

        public override void OnDisable()
        {
            Events.Scp914UpgradeEvent -= EventHandler.SCP914Upgrade;
            Events.PlayerJoinEvent -= EventHandler.PlayerJoin;

            Timing.KillCoroutines(cor);

            Inventories = null;
            roleHealth.Clear();
            scp914Items.Clear();
            scp914Roles.Clear();

            scp914Items = null;
            scp914Roles = null;
            EventHandler = null;
            Instance = null;
            if (HarmonyInstance != null)
            {
                HarmonyInstance.UnpatchAll();
            }
        }

        public static void DebugBoi(string line)
        {
            if (Config.GetBool("util_debug", false))
                Info("CU-DEBUG | " + line);
        }

        public override void OnEnable()
        {
            if (!Config.GetBool("util_enable", true))
                return;

            Info("Loading Common-Utils, created by the EXILED Team!");
            
            Instance = this;

            HarmonyInstance = HarmonyInstance.Create("exiled.common.utils");
            HarmonyInstance.PatchAll();

            bool enable914Configs = Config.GetBool("util_914_enable", true);

            if (enable914Configs)
            {
                Dictionary<string, string> configHealth = KConf.ExiledConfiguration.GetDictonaryValue(Config.GetString("util_role_health", "NtfCommander:400,NtfScientist:350"));

                try
                {
                    foreach (KeyValuePair<string, string> kvp in configHealth)
                    {
                        roleHealth.Add((RoleType)Enum.Parse(typeof(RoleType), kvp.Key), int.Parse(kvp.Value));
                        DebugBoi(kvp.Key + "'s default health is now: " + kvp.Value);
                    }
                    Info("Loaded " + configHealth.Keys.Count() + "('s) default health classes.");
                }
                catch (Exception e)
                {
                    Error("Failed to add custom health to roles. Check your 'util_role_health' config values for errors!\n" + e);
                }

                Dictionary<string, string> configRoles =
                    KConf.ExiledConfiguration.GetDictonaryValue(Config.GetString("util_914_roles", ""));
                try
                {
                    foreach (KeyValuePair<string, string> kvp in configRoles)
                        scp914Roles.Add(Scp914PlayerUpgrade.ParseString(kvp.Key),
                            (Scp914Knob)Enum.Parse(typeof(Scp914Knob), kvp.Value));

                    Info("Loaded " + configRoles.Count + "('s) custom 914 upgrade classes.");
                }
                catch (Exception e)
                {
                    Error($"Failed to parse 914 role upgrade settings. {e}");
                }

                

                Dictionary<string, string> configItems = KConf.ExiledConfiguration.GetDictonaryValue(Config.GetString("util_914_items", "Painkillers-Medkit:Fine,Coin-Flashlight:OneToOne"));

                try
                {
                    foreach (KeyValuePair<string, string> kvp in configItems)
                        scp914Items.Add(Scp914ItemUpgrade.ParseString(kvp.Key), (Scp914Knob)Enum.Parse(typeof(Scp914Knob), kvp.Value));

                    Info("Loaded " + configItems.Count + "('s) custom 914 recipes.");
                }
                catch (Exception e)
                {
                    Error("Failed to add items to 914. Check your 'util_914_items' config values for errors!\n" + e);
                }
            }

            bool enableCustomInv = Config.GetBool("util_enable_inventories", false);

            if (enableCustomInv)
            {
                // Custom items
                try
                {
                    Inventories = new CustomInventory();
                    Inventories.ClassD = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_classd_inventory", null)));
                    Inventories.Chaos = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_chaos_inventory", null)));
                    Inventories.NtfCadet = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntfcadet_inventory", null)));
                    Inventories.NtfCommander = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntfcommander_inventory", null)));
                    Inventories.NtfLieutenant = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntflieutenant_inventory", null)));
                    Inventories.NtfScientist = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntfscientist_inventory", null)));
                    Inventories.Scientist = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_scientist_inventory", null)));
                    Inventories.Guard = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_guard_inventory", null)));
                    Info("Loaded Inventories.");
                }
                catch (Exception e)
                {
                    Error("Failed to add items to custom inventories! Check your inventory config values for errors!\n[EXCEPTION] For Developers:\n" + e);
                    return;
                }
            }

            bool upgradeHeldItems = Config.GetBool("util_914_upgrade_hand", true);

            bool enableBroadcasting = Config.GetBool("util_broadcast_enable", true);

            string broadcastMessage = Config.GetString("util_broadcast_message", "<color=lime>This server is running <b><color=red>EXILED-CommonUtils</color></b>, enjoy playing!</color>");

            int boradcastSeconds = Config.GetInt("util_broadcast_seconds", 300); // 300 is 5 minutes. :D
            int boradcastTime = Config.GetInt("util_broadcast_time", 4);

            string joinMessage = Config.GetString("util_joinMessage", "<color=lime>Welcome <b>%player%</b>! <i>Please read our rules!</i></color>");
            int joinMessageTime = Config.GetInt("util_joinMessage_time", 6); // 6 seconds duhhhhh

            bool enableAutoNuke = Config.GetBool("util_enable_autonuke", false);

            int autoNukeTime = Config.GetInt("util_autonuke_time", 600); // 600 seconds is 10 minutes.
            bool clearRagdolls = Config.GetBool("util_cleanup_ragdolls", true);
            float clearRagdollTimer = Config.GetFloat("util_cleanup_interval", 250f);
            bool clearOnlyPocket = Config.GetBool("util_cleanup_only_pocket", false);
            bool clearItems = Config.GetBool("util_cleanup_items", true);

            EventHandler = new EventHandlers(upgradeHeldItems, scp914Roles, scp914Items, roleHealth, broadcastMessage, joinMessage, boradcastTime, boradcastSeconds, joinMessageTime, Inventories, autoNukeTime, enableAutoNuke, enable914Configs, enableBroadcasting, enableCustomInv, clearRagdolls, clearRagdollTimer, clearOnlyPocket, clearItems)
            { 
                LockAutoNuke = Config.GetBool("util_autonuke_lock", false)
            };
            Events.PlayerJoinEvent += EventHandler.PlayerJoin;
            Events.Scp914UpgradeEvent += EventHandler.SCP914Upgrade;
            Events.RoundStartEvent += EventHandler.RoundStart;
            Events.RoundEndEvent += EventHandler.OnRoundEnd;
            Events.WaitingForPlayersEvent += EventHandler.OnWaitingForPlayers;

            Info("Common-Utils Loaded! Created by the EXILED Team.");

            if (!enableBroadcasting)
                return;

            cor = Timing.RunCoroutine(EventHandler.CustomBroadcast());
        }

        public override void OnReload()
        {

        }
    }
}
