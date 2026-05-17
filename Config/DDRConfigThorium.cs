using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DynamicDamageReductionforBosses.Config
{
    /// <summary>
    /// Thorium Mod Boss 勾选配置。
    /// 始终加载；未加载 Thorium 时勾选无效。
    /// </summary>
    public class DDRConfigThorium : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ── 困难模式前 ───────────────────────────────────────────────
        [Header("PreHardmode")]
        [DefaultValue(false)] public bool TheGrandThunderBird { get; set; }
        [DefaultValue(false)] public bool QueenJellyfish      { get; set; }
        [DefaultValue(false)] public bool Viscount            { get; set; }
        [DefaultValue(false)] public bool GraniteEnergyStorm  { get; set; }
        [DefaultValue(false)] public bool BuriedChampion      { get; set; }
        [DefaultValue(false)] public bool StarScouter         { get; set; }

        // ── 困难模式 ─────────────────────────────────────────────────
        [Header("Hardmode")]
        [DefaultValue(false)] public bool BoreanStrider   { get; set; }
        [DefaultValue(false)] public bool FallenBeholder  { get; set; }
        [DefaultValue(false)] public bool Lich            { get; set; }
        [DefaultValue(false)] public bool ForgottenOne    { get; set; }
        [DefaultValue(false)] public bool ThePrimordials  { get; set; }

        // ── 迷你 Boss ─────────────────────────────────────────────────
        [Header("MiniBoss")]
        [DefaultValue(false)] public bool PatchWerk   { get; set; }
        [DefaultValue(false)] public bool CorpseBloom { get; set; }
        [DefaultValue(false)] public bool Illusionist { get; set; }
    }
}
