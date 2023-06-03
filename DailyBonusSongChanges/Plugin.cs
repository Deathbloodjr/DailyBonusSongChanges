using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using BepInEx.Configuration;
using DailyBonusSongChanges.Patches;

#if TAIKO_IL2CPP
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP;
#endif

namespace DailyBonusSongChanges
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, "Daily Bonus Song Changes", PluginInfo.PLUGIN_VERSION)]
#if TAIKO_MONO
    public class Plugin : BaseUnityPlugin
#elif TAIKO_IL2CPP
    public class Plugin : BasePlugin
#endif
    {
        public static Plugin Instance;
        private Harmony _harmony;
        public new static ManualLogSource Log;

        public ConfigEntry<bool> ConfigEnabled;

        public ConfigEntry<int> ConfigNumDailyBonusSongs;
        public ConfigEntry<int> ConfigMinLevelBonusSongs;
        public ConfigEntry<int> ConfigMaxLevelBonusSongs;

#if TAIKO_MONO
        private void Awake()
#elif TAIKO_IL2CPP
        public override void Load()
#endif
        {
            Instance = this;

#if TAIKO_MONO
            Log = Logger;
#elif TAIKO_IL2CPP
            Log = base.Log;
#endif

            SetupConfig();
            SetupHarmony();
        }

        private void SetupConfig()
        {
            // I never really used this
            // I'd rather just use a folder in BepInEx's folder for storing information
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            ConfigEnabled = Config.Bind("General",
                "Enabled",
                true,
                "Enables the mod.");

            ConfigNumDailyBonusSongs = Config.Bind("BonusSongs",
                "NumDailyBonusSongs",
                3,
                "Change how many daily bonus songs you get");

            ConfigMinLevelBonusSongs = Config.Bind("BonusSongs",
                "MinLevelBonusSongs",
                1,
                "Change the minimum level of bonus songs you get");

            ConfigMaxLevelBonusSongs = Config.Bind("BonusSongs",
                "MaxLevelBonusSongs",
                10,
                "Change the maximum level of daily bonus songs you get");

        }

        private void SetupHarmony()
        {
            // Patch methods
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            // If the mod's enabled, and any of the variables are different from the defaults
            if (ConfigEnabled.Value && (ConfigMinLevelBonusSongs.Value > 1 || ConfigMaxLevelBonusSongs.Value < 10 || ConfigNumDailyBonusSongs.Value != 3))
            {
                // Clamps these values to be between 1 and 10
                ConfigMinLevelBonusSongs.Value = Math.Max(ConfigMinLevelBonusSongs.Value, 1);
                ConfigMaxLevelBonusSongs.Value = Math.Max(ConfigMaxLevelBonusSongs.Value, 1);
                ConfigMinLevelBonusSongs.Value = Math.Min(ConfigMinLevelBonusSongs.Value, 10);
                ConfigMaxLevelBonusSongs.Value = Math.Min(ConfigMaxLevelBonusSongs.Value, 10);
                // You should be allowed to have 0 bonus songs, negative should be capped to 0
                // Probably should have a higher cap too, but whatever, I'll see if that crashes
                ConfigNumDailyBonusSongs.Value = Math.Max(ConfigNumDailyBonusSongs.Value, 0);

                _harmony.PatchAll(typeof(DailyBonusSongChangesPatch));
                Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled.");
            }
        }

        // I never used these, but they may come in handy at some point
        public static MonoBehaviour GetMonoBehaviour() => TaikoSingletonMonoBehaviour<CommonObjects>.Instance;

        public void StartCustomCoroutine(IEnumerator enumerator)
        {
#if TAIKO_MONO
            GetMonoBehaviour().StartCoroutine(enumerator);
#elif TAIKO_IL2CPP
            GetMonoBehaviour().StartCoroutine(enumerator);
#endif
        }

    }
}