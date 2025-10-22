using System;
using System.Collections.Generic;
using System.Text;

namespace EngineersCrest
{
    public class EngineerTurretTool : ToolItemBasic
    {
        public static ToolItemsData.Data defaultData = new()
        {
            IsUnlocked = false,
            IsHidden = true,
            HasBeenSeen = true,
            HasBeenSelected = true,
            AmountLeft = 0,
        };
        public EngineerTurretTool()
        {
            //baseStorageAmount = 3;
            usageOptions.ThrowCooldown = 0.25f;
            usageOptions.FsmEventName = "";
            usageOptions.ThrowPrefab = EngineersCrestPlugin.TurretSpawnerPrefab;
            name = "ENGINEERTURRET";
            description = new() { Key = $"{name}TOOLDESC", Sheet = $"{name}TOOL" };
            displayName = new() { Key = $"{name}TOOLNAME", Sheet = $"{name}TOOL" };
            type = ToolItemType.Red;
            baseStorageAmount = 0;
            alternateUnlockedTest = new PlayerDataTest();
            SavedData = defaultData;
        }
        public override void OnUnlocked()
        {
            base.OnUnlocked();
            Lock();
        }
        public override void OnWasUsed(bool wasEmpty)
        {
            base.OnWasUsed(wasEmpty);
            if (!wasEmpty)
            {
                ToolItem? tool = Patches.GetWillThrowTool();
                if (tool != null && !Turret.GetTurretForTool(tool)) tool.CustomUsage(1);
            }
        }
    }
}
