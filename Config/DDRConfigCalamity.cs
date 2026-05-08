using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DynamicDamageReductionforBosses.Config
{
    /// <summary>
    /// 灾厄 Boss 勾选配置。
    /// 始终加载（ModConfig 不支持条件注册）。
    /// 未加载灾厄时勾选无效，BossFightTracker 不会注册灾厄 NPC，什么也不会发生。
    /// 全局设置（最短击杀时间、血量倍数等）沿用主 DDRConfig。
    /// </summary>
    public class DDRConfigCalamity : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ── 困难模式前 ───────────────────────────────────────────────
        [Header("PreHardmode")]
        [DefaultValue(false)] public bool DesertScourge        { get; set; }
        [DefaultValue(false)] public bool Crabulon             { get; set; }
        [DefaultValue(false)] public bool HiveMind             { get; set; }
        [DefaultValue(false)] public bool Perforators          { get; set; }
        [DefaultValue(false)] public bool SlimeGod             { get; set; }

        // ── 困难模式 ─────────────────────────────────────────────────
        [Header("Hardmode")]
        [DefaultValue(false)] public bool Cryogen              { get; set; }
        [DefaultValue(false)] public bool AquaticScourge       { get; set; }
        [DefaultValue(false)] public bool BrimstoneElemental   { get; set; }
        [DefaultValue(false)] public bool CalamitasClone       { get; set; }
        [DefaultValue(false)] public bool Leviathan            { get; set; }
        [DefaultValue(false)] public bool AstrumAureus         { get; set; }
        [DefaultValue(false)] public bool PlaguebringerGoliath { get; set; }
        [DefaultValue(false)] public bool Ravager              { get; set; }
        [DefaultValue(false)] public bool AstrumDeus           { get; set; }

        // ── 月亮领主后 ───────────────────────────────────────────────
        [Header("PostMoonLord")]
        [DefaultValue(false)] public bool ProfanedGuardians    { get; set; }
        [DefaultValue(false)] public bool Dragonfolly          { get; set; }
        [DefaultValue(false)] public bool Providence           { get; set; }
        [DefaultValue(false)] public bool StormWeaver          { get; set; }
        [DefaultValue(false)] public bool CeaselessVoid        { get; set; }
        [DefaultValue(false)] public bool Signus               { get; set; }
        [DefaultValue(false)] public bool Polterghast          { get; set; }
        [DefaultValue(false)] public bool OldDuke              { get; set; }
        [DefaultValue(false)] public bool DevourerofGods       { get; set; }
        [DefaultValue(false)] public bool Yharon               { get; set; }
        [DefaultValue(false)] public bool ExoMechs             { get; set; }
        [DefaultValue(false)] public bool SupremeCalamitas     { get; set; }
    }
}
