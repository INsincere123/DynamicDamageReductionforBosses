using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DynamicDamageReductionforBosses.Config
{
    /// <summary>
    /// Homeward Journey boss selection config.
    /// </summary>
    public class DDRConfigContinentOfJourney : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("PreHardmode")]
        [DefaultValue(false)] public bool GoblinChariot     { get; set; }
        [DefaultValue(false)] public bool BigDipper         { get; set; }
        [DefaultValue(false)] public bool PuppetOpera       { get; set; }
        [DefaultValue(false)] public bool MarquisMoonsquid  { get; set; }

        [Header("Hardmode")]
        [DefaultValue(false)] public bool PriestessRod      { get; set; }
        [DefaultValue(false)] public bool Diver             { get; set; }
        [DefaultValue(false)] public bool TheMotherbrain    { get; set; }
        [DefaultValue(false)] public bool WallofShadow      { get; set; }

        [Header("PostWallofShadow")]
        [DefaultValue(false)] public bool SlimeGod          { get; set; }
        [DefaultValue(false)] public bool TheOverwatcher    { get; set; }
        [DefaultValue(false)] public bool TheLifebringer    { get; set; }
        [DefaultValue(false)] public bool TheMaterealizer   { get; set; }
        [DefaultValue(false)] public bool ScarabBelief      { get; set; }
        [DefaultValue(false)] public bool WorldsEndEverlastingFallingWhale { get; set; }
        [DefaultValue(false)] public bool TheSon            { get; set; }
    }
}
