using System;
using System.Collections.Generic;
using System.ComponentModel;
using DynamicDamageReductionforBosses.Systems;
using Terraria.ModLoader.Config;

namespace DynamicDamageReductionforBosses.Config
{
    [Serializable]
    public class CustomBossEntry : IEquatable<CustomBossEntry>
    {
        // 模组内部名，如 CalamityMod
        [DefaultValue("")]
        public string ModName { get; set; } = "";

        // NPC 类名，如 SupremeCalamitas
        [DefaultValue("")]
        public string ClassName { get; set; } = "";

        // 分组名：同一场战斗的多段 Boss 填相同的值，单体留空（自动用类名）
        [DefaultValue("")]
        public string GroupKey { get; set; } = "";

        // 勾选 = 致死 NPC（受硬地板 + CheckDead 保护），不勾 = 只受软减伤（可自然死亡）
        [DefaultValue(true)]
        public bool IsKillNpc { get; set; } = true;

        public bool Equals(CustomBossEntry other) =>
            other != null &&
            ModName    == other.ModName    &&
            ClassName  == other.ClassName  &&
            GroupKey   == other.GroupKey   &&
            IsKillNpc  == other.IsKillNpc;

        public override bool Equals(object obj) => Equals(obj as CustomBossEntry);

        public override int GetHashCode() =>
            HashCode.Combine(ModName, ClassName, GroupKey, IsKillNpc);
    }

    public class DDRConfigCustom : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public List<CustomBossEntry> CustomBosses { get; set; } = new();

        public override void OnChanged()
        {
            // PostSetupContent 完成前不执行（其他模组的 NPC 尚未注册）
            if (!BossFightTracker.IsInitialized) return;
            BossFightTracker.RegisterCustomBosses(this);
        }
    }
}
