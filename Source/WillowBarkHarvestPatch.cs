using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

public class Main
{
    private static Harmony harmony;

    public static void Load()
    {
        try
        {
            harmony = new Harmony("com.survivalist.willowbark");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("[WillowBark] Successfully loaded Harmony patch for FallenTreeProp.ChopWood.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[WillowBark] Failed to apply patches: " + ex);
        }
    }

    public static void Unload()
    {
        try
        {
            if (harmony != null)
            {
                harmony.UnpatchAll("com.survivalist.willowbark");
                harmony = null;
                Debug.Log("[WillowBark] Unloaded patches.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[WillowBark] Failed to unload patches: " + ex);
        }
    }
}

public static class WillowBarkHelper
{
    public const float DropChance = 0.80f; // Rare occasion: ~20% chance per chop

    public static EquipmentPrototype FindBarkPrototype()
    {
        try
        {
            if (GameImpl.Instance != null)
            {
                EquipmentPrototype p = GameImpl.Instance.FindEquipmentPrototypeByName("WillowBark");
                if (p != null) return p;
                p = GameImpl.Instance.FindEquipmentPrototypeByName("Willow Bark");
                if (p != null) return p;

                if (GameImpl.Instance.CurrentStories != null)
                {
                    foreach (Story s in GameImpl.Instance.CurrentStories)
                    {
                        if (s != null && s.EquipmentPrototypes != null)
                        {
                            if (s.EquipmentPrototypes.TryGetValue("WillowBark", out p)) return p;
                            if (s.EquipmentPrototypes.TryGetValue("Willow Bark", out p)) return p;
                            foreach (var kvp in s.EquipmentPrototypes)
                            {
                                if (kvp.Value != null && (kvp.Value.NativeName == "Willow Bark" || kvp.Value.Name == "WillowBark"))
                                {
                                    return kvp.Value;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[WillowBark] Error finding bark prototype: " + ex);
        }
        return null;
    }

    public static void TryDropWillowBark(TileObject chopper, TileObject treeSource, string sourceAction)
    {
        try
        {
            float roll = UnityEngine.Random.value;
            Debug.Log(string.Format("[WillowBark] {0} called by {1}. Roll: {2:F2} (needed <= {3:F2})",
                sourceAction,
                chopper != null ? chopper.ToString() : "unknown",
                roll,
                DropChance));

            if (roll > DropChance)
            {
                return;
            }

            EquipmentPrototype barkProto = FindBarkPrototype();
            if (barkProto == null)
            {
                Debug.LogError("[WillowBark] Could not locate WillowBark EquipmentPrototype in any active story!");
                return;
            }

            int amount = UnityEngine.Random.Range(1, 4); // 1 to 3 Willow Bark
            Character character = chopper as Character;

            if (character != null && character.Inventory != null)
            {
                Equipment bark = Equipment.Spawn(barkProto, amount);
                if (bark != null)
                {
                    Equipment remainder = character.Inventory.Add(character, bark);
                    if (remainder != null && remainder.GetAmount() > 0 && treeSource != null)
                    {
                        treeSource.SpawnEquipmentIfSpaceIsAvailable(barkProto, remainder.GetAmount(), false);
                    }
                    if (NotificationManager.Instance != null)
                    {
                        NotificationManager.Instance.AddEquipmentNotification(null, character, bark, amount);
                    }
                    Debug.Log(string.Format("[WillowBark] Successfully awarded {0} Willow Bark to {1}!", amount, character.ToString()));
                }
            }
            else if (treeSource != null)
            {
                treeSource.SpawnEquipmentIfSpaceIsAvailable(barkProto, amount, false);
                Debug.Log(string.Format("[WillowBark] Successfully spawned {0} Willow Bark at tree location!", amount));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[WillowBark] Error executing TryDropWillowBark: " + ex);
        }
    }
}

// Hook strictly when chopping a fallen tree log
[HarmonyPatch(typeof(FallenTreeProp), "ChopWood")]
public static class FallenTreeProp_ChopWood_Patch
{
    [HarmonyPostfix]
    public static void Postfix(FallenTreeProp __instance, Character character)
    {
        WillowBarkHelper.TryDropWillowBark(character, __instance, "ChopWood");
    }
}
