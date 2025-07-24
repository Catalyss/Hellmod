using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PlayerData;

namespace Hellmod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        var harmony = new Harmony("com.catalyss.hellmod");
        harmony.PatchAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private GameObject updaterGO;


    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (!ProfileCheck()) return;

        foreach (var item in FindObjectsByType<LostLootMachine>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
        {
            item.gameObject.SetActive(false);
        }
        if (updaterGO == null)
        {
            updaterGO = new GameObject("CustomSceneLoaderUpdater");
            updaterGO.AddComponent<UpdateBehaviour>();
            DontDestroyOnLoad(updaterGO);
        }

    }
    /*
        [HarmonyPatch(typeof(MissionManager), "OnFailMission_Client")]
        public class Patch_MissionManager_OnFailMission_Client
        {
            static void Postfix(MissionManager.MissionState state, int score)
            {
                if (!ProfileCheck()) return;

                Logger.LogWarning("Mission failed - resetting PlayerData and PlayerResources");

                var playerData = Instance;
                if (playerData == null)
                {
                    Logger.LogWarning("PlayerData.Instance is null!");
                    return;
                }

                var allResources = UnityEngine.Object.FindObjectsOfType<PlayerResource>();
                foreach (var resource in allResources)
                {
                    Logger.LogWarning(resource.name + " " + (playerData.GetResource(resource) - 1));
                    if (!playerData.TryRemoveResource(resource, playerData.GetResource(resource) - 1))
                    {
                        Logger.LogError(resource.name + " Failled to remove");
                    }
                }

                try
                {
                    var type = typeof(PlayerData);

                    var unlockStateField = type.GetField("unlockState", BindingFlags.Instance | BindingFlags.NonPublic);
                    var gearField = type.GetField("gear", BindingFlags.Instance | BindingFlags.NonPublic);
                    var gearValue = gearField?.GetValue(playerData);
                    var infoProp = gearValue?.GetType().GetProperty("Info", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var unlockStateValue = infoProp?.GetValue(gearValue)?.GetType().GetProperty("UnlockState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(infoProp.GetValue(gearValue));
                    unlockStateField?.SetValue(playerData, unlockStateValue);
                    Logger.LogWarning("Reset unlockState");

                    var levelField = type.GetField("level", BindingFlags.Instance | BindingFlags.NonPublic);
                    levelField?.SetValue(playerData, 1);
                    Logger.LogWarning("Reset level to 1");

                    var levelXPField = type.GetField("levelXP", BindingFlags.Instance | BindingFlags.NonPublic);
                    levelXPField?.SetValue(playerData, 0);
                    Logger.LogWarning("Reset levelXP to 0");

                    var claimedUnlocksField = type.GetField("claimedLevelUnlocks", BindingFlags.Instance | BindingFlags.NonPublic);
                    var claimedUnlocksValue = claimedUnlocksField?.GetValue(playerData) as System.Collections.ICollection;
                    if (claimedUnlocksValue != null)
                    {
                        var clearMethod = claimedUnlocksValue.GetType().GetMethod("Clear");
                        clearMethod?.Invoke(claimedUnlocksValue, null);
                        Logger.LogWarning("Cleared claimedLevelUnlocks");
                    }

                    var hasBeenSeenField = type.GetField("hasBeenSeen", BindingFlags.Instance | BindingFlags.NonPublic);
                    hasBeenSeenField?.SetValue(playerData, false);
                    Logger.LogWarning("Set hasBeenSeen to false");

                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error resetting PlayerData fields: {ex}");
                }
            }
        }
    */


    [HarmonyPatch(typeof(Global), "Initialize")]
    public class Patch_Global_Initialize
    {
        static void Postfix(Global __instance)
        {
            if (!ProfileCheck()) return;

            if (__instance == null)
            {
                Logger.LogWarning("[BigMonoLobby] Global instance was null in Start");
                return;
            }

            // Set MissionModifierChance to 1
            __instance.MissionModifierChance = 1000;
            Logger.LogInfo("MissionModifierChance set to 1");
            for (int i = 0; i < __instance.MissionModifierCountCurve.keys.Length; i++)
            {
                __instance.MissionModifierCountCurve.keys[i].value = 300;
            }
            // Example: Update MissionModifier index 1 to level 10
            //var currentModifier = __instance.MissionModifiers.Get(1);
            //__instance.MissionModifiers.Set(1, currentModifier, 1000);
            for (int i = 0; i < __instance.MissionModifiers.Length; i++)
            {
                Logger.LogInfo($"MissionModifier {__instance.MissionModifiers.Get(i).ModifierName} GetWeight {__instance.MissionModifiers.GetWeight(i)}");

            }
        }
    }

    [HarmonyPatch(typeof(GameManager), "Start")]
    public static class Patch_GameManager_Start
    {
        static void Postfix()
        {
            if (!ProfileCheck()) return;

            var playerData = Instance;
            if (playerData == null)
            {
                Logger.LogWarning("PlayerData.Instance is null!");
                return;
            }

            var __instance = Global.Instance;

            if (__instance == null)
            {
                Logger.LogWarning("[Hellmod] Global instance was null in Start");
                return;
            }

            // Set MissionModifierChance to 1
            __instance.MissionModifierChance = 1000;
            Logger.LogInfo("MissionModifierChance set to 1");
            var keys = __instance.MissionModifierCountCurve.keys;

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].value = (keys[i].value + 1) * 20; // Set all keyframe values to 3000
            }

            // Reassign modified keys back to the curve
            __instance.MissionModifierCountCurve.keys = keys;
            for (int i = 0; i < __instance.MissionModifiers.Length; i++)
            {
                var modifier = __instance.MissionModifiers.Get(i); // Get MissionModifier instance

                // Access the private `flags` field
                var flagsField = typeof(MissionModifier).GetField("flags", BindingFlags.Instance | BindingFlags.NonPublic);
                if (flagsField != null)
                {
                    var currentFlags = (ModifierFlags)flagsField.GetValue(modifier);
                    currentFlags = ModifierFlags.AllowInNormalMissions;
                    flagsField.SetValue(modifier, currentFlags);

                    Logger.LogInfo($"Flags for {modifier.name} set to {currentFlags}");
                }

                // Access private `xpMultiplier` field
                var xpMultiplierField = typeof(MissionModifier).GetField("xpMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
                if (xpMultiplierField != null)
                {
                    xpMultiplierField.SetValue(modifier, 0); // Or whatever multiplier you want
                    Logger.LogInfo($"{modifier.name} XP multiplier set to 100x");
                }

                // Set weight
                __instance.MissionModifiers.SetWeight(i, 100);
            }

        }
    }

    public static bool ProfileCheck()
    {
        // Access the PlayerData.ProfileConfig instance
        var profileConfig = PlayerData.ProfileConfig.Instance;
        if (profileConfig == null)
        {
            Logger.LogWarning("PlayerData.ProfileConfig is null!");
            return false;
        }

        // Use reflection to get the private field
        var field = typeof(ProfileConfig).GetField("profileIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Logger.LogWarning("Could not find private field 'profileIndex'");
            return false;
        }

        // Read the current value
        int profileIndex = (int)field.GetValue(profileConfig);

        // Check value
        if (profileIndex < 7)
        {
            Logger.LogWarning($"profileIndex = {profileIndex}, skipping");
            return false;
        }

        return true;
    }


    [HarmonyPatch(typeof(StartMenu), "Start")]
    public static class Patch_StartMenu_Start
    {
        static void Postfix()
        {
            StartMenu.Instance.transform.GetChild(1).GetChild(21).GetChild(8).GetComponent<TextMeshProUGUI>().text = "<color=\"red\">HELLMOD";

            if (!ProfileCheck()) return;

            if (Plugin.lg == null) LoadEmbeddedAssetBundle();

            if (StartMenu.Instance == null) return;
            StartMenu.Instance.transform.GetChild(1).GetChild(9).GetComponent<Image>().sprite = lg;
            StartMenu.Instance.transform.GetChild(1).GetChild(14).GetChild(0).GetComponent<TextMeshProUGUI>().text = "<color=\"red\">HELLMOD";

        }
    }
    public static AssetBundle assets;
    public static Sprite lg;
    public static void LoadEmbeddedAssetBundle()
    {
        Logger.LogInfo("Loading embedded AssetBundle...");

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("hellmod")); // find 'gm'

        if (resourceName == null)
        {
            Logger.LogError("Embedded resource 'hellmod' not found!");
            return;
        }

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                Logger.LogError("Failed to get stream for embedded AssetBundle 'hellmod'.");
                return;
            }

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                assets = AssetBundle.LoadFromMemory(ms.ToArray());
            }
        }

        if (assets == null)
        {
            Logger.LogError("Failed to load AssetBundle from memory.");
            return;
        }

        Logger.LogInfo("AssetBundle 'hellmod' loaded successfully.");

        // Load your prefab from the bundle (replace with actual prefab name)
        lg = assets.LoadAsset<Sprite>("hellmod_logo");

        if (lg == null)
        {
            Logger.LogError("Failed to load 'hellmod_logo' prefab from AssetBundle.");
        }
        else
        {
            Logger.LogInfo("Prefab 'hellmod_logo' loaded successfully.");
        }
    }
    public static GameObject fuck;
    public class UpdateBehaviour : MonoBehaviour
    {
        public Vector3 size;
        void Update()
        {
            if (!ProfileCheck()) return;

            if (fuck != null)
            {
                Vector3 centerpoint = Vector3.zero;
                int validPlayerCount = 0;

                for (int i = 0; i < GameManager.players.Count; i++)
                {
                    if (GameManager.players[i] == null) continue;

                    centerpoint += GameManager.players[i].transform.position;
                    validPlayerCount++;
                }

                if (validPlayerCount > 0)
                {
                    centerpoint /= validPlayerCount;
                    fuck.transform.position = centerpoint;
                    fuck.transform.localScale = DivVectors(size, Vector3.one + (new Vector3(0.05f, 0.05f, 0.05f) * validPlayerCount));
                }
            }
            if (fuck == null && GameManager.players != null &&GameManager.players[0] != null)
            {
                if (!Resources.FindObjectsOfTypeAll<SafetyBubble>()[0])
                {
                    Debug.LogError("SafetyNot found");
                    return;
                }
                if (!Resources.FindObjectsOfTypeAll<SafetyBubble>()[0].gameObject)
                {
                    Debug.LogError("SafetyNot gameObject found");
                    return;
                }
                fuck = Instantiate(Resources.FindObjectsOfTypeAll<SafetyBubble>()[0].gameObject);
                size = fuck.transform.localScale;
            }
        }
        public Vector3 DivVectors(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

    }
}