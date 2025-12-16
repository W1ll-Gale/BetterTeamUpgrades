using BepInEx.Configuration;


namespace BetterTeamUpgrades.Config
{
    internal class Configuration
    {
        public static ConfigEntry<bool> EnableSharedUpgradesPatch;
        public static ConfigEntry<int> SharedUpgradeChance;
        public static ConfigEntry<bool> EnableLateJoinPlayerUpdateSyncPatch;
        public static ConfigEntry<int> LateJoinUpgradeSyncChance;
        public static ConfigEntry<bool> EnableCustomUpgradeSyncing;

        public static void Init(ConfigFile config)
        {
            EnableSharedUpgradesPatch = config.Bind<bool>(
                "Shared Upgrade Settings",
                "EnableSharedUpgrades",
                true,
                "Enables Shared Upgrades for all supported Upgrades"
            );

            SharedUpgradeChance = config.Bind<int>(
                new ConfigDefinition(
                    "Shared Upgrade Settings",
                    "SharedUpgradeChance"
                ),
                100,
                new ConfigDescription(
                    "The percentage chance (0-100) that an upgrade will be shared with team members when purchased.",
                    new AcceptableValueRange<int>(0, 100)
                )
            );

            EnableLateJoinPlayerUpdateSyncPatch = config.Bind<bool>(
                "Late Join Settings",
                "EnableLateJoinPlayerUpgradeSync",
                false,
                "Enables Upgrade Sync for Late Joining Players"
            );

            LateJoinUpgradeSyncChance = config.Bind<int>(
                new ConfigDefinition(
                    "Late Join Settings",
                    "LateJoinUpgradeSyncChance"
                ),
                100,
                new ConfigDescription(
                    "The percentage chance (0-100) that a late joining player will receive each upgrade their team members have.",
                    new AcceptableValueRange<int>(0, 100)
                )
            );

            EnableCustomUpgradeSyncing = config.Bind<bool>(
                "Extra Sync Settings",
                "EnableCustomUpgradeSyncing",
                true,
                "Enables Custom Upgrade Syncing for Modded Upgrades (may cause issues with some mods)"
            );
        }
    }
}