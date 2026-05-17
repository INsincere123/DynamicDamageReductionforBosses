using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DynamicDamageReductionforBosses.Config
{
    /// <summary>
    /// Fargo's Souls Mod Boss 勾选配置。
    /// 始终加载（ModConfig 不支持条件注册）。
    /// 未加载 Fargo 时勾选无效，BossFightTracker 不会注册 Fargo NPC，什么也不会发生。
    /// 全局设置（最短击杀时间、血量倍数等）沿用主 DDRConfig。
    /// </summary>
    public class DDRConfigFargo : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ── 主线 Boss ────────────────────────────────────────────────
        [Header("Bosses")]
        [DefaultValue(false)] public bool TrojanSquirrel  { get; set; }
        [DefaultValue(false)] public bool CursedCoffin    { get; set; }
        [DefaultValue(false)] public bool DeviBoss        { get; set; }
        [DefaultValue(false)] public bool BanishedBaron   { get; set; }
        [DefaultValue(false)] public bool LifeChallenger  { get; set; }
        [DefaultValue(false)] public bool CosmosChampion  { get; set; }
        [DefaultValue(false)] public bool AbomBoss        { get; set; }
        [DefaultValue(false)] public bool MutantBoss      { get; set; }

        // ── 迷你 Boss ────────────────────────────────────────────────
        [Header("MiniBosses")]
        [DefaultValue(false)] public bool TimberChampion  { get; set; }
        [DefaultValue(false)] public bool TerraChampion   { get; set; }
        [DefaultValue(false)] public bool EarthChampion   { get; set; }
        [DefaultValue(false)] public bool NatureChampion  { get; set; }
        [DefaultValue(false)] public bool LifeChampion    { get; set; }
        [DefaultValue(false)] public bool SpiritChampion  { get; set; }
        [DefaultValue(false)] public bool ShadowChampion  { get; set; }
        [DefaultValue(false)] public bool WillChampion    { get; set; }
    }
}
