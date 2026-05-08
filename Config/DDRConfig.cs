using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DynamicDamageReductionforBosses.Config
{
    public class DDRConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ── 功能开关 ──────────────────────────────────────────────────
        [Header("General")]

        // 动态减伤开关：强制 Boss 不早于最短击杀时间死亡
        [DefaultValue(false)]
        public bool EnableDamageReduction { get; set; }

        [Range(5f, 600f)]
        [Increment(5f)]
        [DefaultValue(60f)]
        public float MinKillSeconds { get; set; }

        [Range(0.5f, 5f)]
        [Increment(0.5f)]
        [DefaultValue(2f)]
        public float CurvePower { get; set; }

        // 血量倍数开关：对选中 Boss 的最大血量乘以指定倍数
        [DefaultValue(false)]
        public bool EnableHpMultiplier { get; set; }

        // 血量倍数：1 = 原版，10 = 十倍血量，上限 10000
        // 可拖动滑条粗调，或选中后用左右方向键精细调节（每次 ±1）
        [Range(1, 10000)]
        [Increment(1)]
        [DefaultValue(1)]
        public int HpMultiplier { get; set; }

        // ──困难模式前 Boss ─────────────────────────────────────────────
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
