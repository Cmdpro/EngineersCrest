using HarmonyLib;
using Needleforge;
using Needleforge.Makers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EngineersCrest
{
    [HarmonyPatch]
    public class Patches
    {
        [HarmonyPatch]
        public class ToolItemManagerPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ToolItemManager), "GetAttackToolBinding")]
            static void GetAttackToolBindingPrefix(ToolItemManager __instance, ref ToolItem tool)
            {
                if (tool == EngineersCrestPlugin.engineerTurret)
                {
                    ToolItem? willThrowTool = GetWillThrowTool();
                    if (willThrowTool != null) tool = willThrowTool;
                }
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ToolItemManager), "IsCustomToolOverride", MethodType.Getter)]
            static bool IsCustomToolOverridePrefix(ToolItemManager __instance, ref bool __result)
            {
                if (customToolOverride)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch]
        public class InfoDumpingPatch
        {
            static bool infoDump = true;
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ToolItemManager), "Awake")]
            static void ToolItemManagerAwake(ToolItemManager __instance)
            {
                if (!infoDump)
                {
                    return;
                }
                foreach (ToolCrest i in ToolItemManager.GetAllCrests())
                {
                    EngineersCrestPlugin.Logger.LogInfo("--- \"" + i.name + "\" ---");
                    EngineersCrestPlugin.Logger.LogInfo(i.crestSprite ? i.crestSprite.name : "Nothing");
                    EngineersCrestPlugin.Logger.LogInfo(i.crestSilhouette ? i.crestSilhouette.name : "Nothing");
                    EngineersCrestPlugin.Logger.LogInfo(i.crestGlow ? i.crestGlow.name : "Nothing");
                    EngineersCrestPlugin.Logger.LogInfo(string.Join(" | ", i.slots.Select((a) => "[" + a.Type + "/" + a.Position + "]")));
                }
            }
        }
        private static ToolItem? willThrowTool;
        public static ToolItem? GetWillThrowTool()
        {
            return willThrowTool;
        }
        static bool customToolOverride = false;
        [HarmonyPatch]
        public class HeroControllerPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HeroController), "GetWillThrowTool")]
            static void GetWillThrowToolPostfix(HeroController __instance, ref bool __result)
            {
                if (EngineersCrestPlugin.engineerCrest.IsEquipped)
                {
                    willThrowTool = __instance.willThrowTool;
                    if (willThrowTool != null) __instance.willThrowTool = EngineersCrestPlugin.engineerTurret; else willThrowTool = null;
                }
                else
                {
                    willThrowTool = null;
                }
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HeroController), "ThrowTool")]
            static void ThrowToolPrefix(HeroController __instance)
            {
                if (EngineersCrestPlugin.engineerCrest.IsEquipped)
                {
                    customToolOverride = true;
                }
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HeroController), "ThrowTool")]
            static void ThrowToolPostfix(HeroController __instance)
            {
                if (EngineersCrestPlugin.engineerCrest.IsEquipped)
                {
                    if (__instance.willThrowTool == null)
                    {
                        willThrowTool = null;
                    }
                    customToolOverride = false;
                }
                else
                {
                    willThrowTool = null;
                }
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HeroController), "CanThrowTool", [typeof(ToolItem), typeof(AttackToolBinding), typeof(bool)])]
            static bool CanThrowToolPrefix(HeroController __instance, ref bool __result, ToolItem tool, AttackToolBinding binding, bool reportFailure)
            {
                if (tool == EngineersCrestPlugin.engineerTurret)
                {
                    if (!tool.IsEmpty)
                    {
                        __result = true;
                        return false;
                    }
                    if (reportFailure)
                    {
                        ToolItemManager.ReportBoundAttackToolFailed(binding);
                    }
                    __result = false;
                    return false;
                }
                return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HeroController), nameof(HeroController.Start))]
            public static void AddTools(HeroController __instance)
            {
                EngineersCrestPlugin.engineerTurret = new EngineerTurretTool();
                ToolMaker.AddCustomTool(EngineersCrestPlugin.engineerTurret);
            }
        }
        [HarmonyPatch]
        public class ToolItemPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ToolItem), "BaseStorageAmount", MethodType.Getter)]
            static bool GetBaseStorageAmountPrefix(ToolItem __instance, ref int __result)
            {
                if (EngineersCrestPlugin.engineerCrest.IsEquipped && __instance.replenishResource != ToolItem.ReplenishResources.Money)
                {
                    if (__instance.type == ToolItemType.Red && __instance.HasLimitedUses())
                    {
                        __result = 3;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
