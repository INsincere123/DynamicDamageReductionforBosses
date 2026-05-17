using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DynamicDamageReductionforBosses.Config
{
    public class DDRConfigVanilla : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ── 功能开关 ──────────────────────────────────────────────────
        [Header("General")]

        [DefaultValue(false)]
        public bool EnableDamageReduction { get; set; }

        [Range(5f, 600f)]
        [Increment(5f)]
        [DefaultValue(60f)]
        public float MinKillSeconds { get; set; }

        // tanh 敏感度：值越大，DPS 超标时减伤越急剧
        [Range(0.5f, 5f)]
        [Increment(0.5f)]
        [DefaultValue(2f)]
        public float Sensitivity { get; set; }

        // 保底系数：无论超出多少，实际伤害最低保留该比例
        [Range(0.01f, 0.5f)]
        [Increment(0.01f)]
        [DefaultValue(0.05f)]
        public float MinDamageRatio { get; set; }

        // 第一阶段时间占比（仅对含非致死部位的多段 Boss 生效）
        // 示例：0.5 = 前 50% 时间用于击杀手/臂等非致死部位，后 50% 用于击杀核心/头
        // 单体 Boss 或全为致死 NPC 的 Boss（双子等）忽略此项
        [Range(0.1f, 0.9f)]
        [Increment(0.05f)]
        [DefaultValue(0.5f)]
        public float PhaseRatio { get; set; }

        [DefaultValue(false)]
        public bool EnableHpMultiplier { get; set; }

        [Range(1, 10000)]
        [Increment(1)]
        [DefaultValue(1)]
        public int HpMultiplier { get; set; }

        // ── 困难模式前 Boss ─────────────────────────────────────────────
        [Header("PreHardmode")]
        [DefaultValue(false)] public bool KingSlime       { get; set; }
        [DefaultValue(false)] public bool EyeOfCthulhu   { get; set; }
        [DefaultValue(false)] public bool EaterOfWorlds  { get; set; }
        [DefaultValue(false)] public bool BrainOfCthulhu { get; set; }
        [DefaultValue(false)] public bool QueenBee        { get; set; }
        [DefaultValue(false)] public bool Skeletron       { get; set; }
        [DefaultValue(false)] public bool Deerclops       { get; set; }
        [DefaultValue(false)] public bool WallOfFlesh     { get; set; }

        // ── 困难模式 Boss ───────────────────────────────────────────────
        [Header("Hardmode")]
        [DefaultValue(false)] public bool QueenSlime     { get; set; }
        [DefaultValue(false)] public bool TheTwins       { get; set; }
        [DefaultValue(false)] public bool TheDestroyer   { get; set; }
        [DefaultValue(false)] public bool SkeletronPrime { get; set; }
        [DefaultValue(false)] public bool Plantera        { get; set; }
        [DefaultValue(false)] public bool Golem           { get; set; }
        [DefaultValue(false)] public bool DukeFishron     { get; set; }
        [DefaultValue(false)] public bool EmpressOfLight  { get; set; }
        [DefaultValue(false)] public bool LunaticCultist { get; set; }
        [DefaultValue(false)] public bool MoonLord       { get; set; }
    }
}
