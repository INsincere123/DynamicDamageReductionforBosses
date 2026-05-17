using System;
using DynamicDamageReductionforBosses.Config;
using DynamicDamageReductionforBosses.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DynamicDamageReductionforBosses.GlobalNPCs
{
    public class DDRGlobalNPC : GlobalNPC
    {
        // ── Boss 生成 ─────────────────────────────────────────────────

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            var config = ModContent.GetInstance<DDRConfig>();
            if (config == null) return;
            if (!BossFightTracker.NpcTypeToFightKey.TryGetValue(npc.type, out string key)) return;
            if (!IsBossSelected(key, config)) return;

            if (config.EnableHpMultiplier)
            {
                int multiplier = Math.Max(1, Math.Min(2000, config.HpMultiplier));
                if (multiplier > 1)
                {
                    npc.lifeMax = (int)Math.Min((long)npc.lifeMax * multiplier, int.MaxValue);
                    npc.life    = npc.lifeMax;
                }
            }

            if (config.EnableDamageReduction)
                BossFightTracker.RegisterSpawn(npc);
        }

        // ── 减伤应用 ──────────────────────────────────────────────────

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
            => ApplyReduction(npc, ref modifiers);

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
            => ApplyReduction(npc, ref modifiers);

        // ── 致死 NPC 最终兜底 ─────────────────────────────────────────

        public override bool CheckDead(NPC npc)
        {
            // 只保护致死 NPC；非致死部位允许自然死亡（脚本触发、相位切换等）
            if (!BossFightTracker.KillNpcTypes.Contains(npc.type)) return true;

            var config = ModContent.GetInstance<DDRConfig>();
            if (config == null || !config.EnableDamageReduction) return true;
            if (!BossFightTracker.NpcTypeToFightKey.TryGetValue(npc.type, out string key)) return true;
            if (!IsBossSelected(key, config)) return true;

            var state = BossFightTracker.GetFightState(npc);
            if (state == null || !state.InitialHP.TryGetValue(npc.whoAmI, out int initialHP)) return true;
            if (initialHP <= 0) return true;

            // 全局 T2/T1 地板——与 ApplyReduction 中致死 NPC 的硬地板保持一致
            float t2 = (Main.GameUpdateCount - state.StartTick) / 60f;
            if (t2 >= config.MinKillSeconds) return true;

            float ratio = Math.Max(1f - t2 / config.MinKillSeconds, 0f);
            npc.life = Math.Max(1, (int)(initialHP * ratio));
            return false;
        }

        // ─────────────────────────────────────────────────────────────

        private static void ApplyReduction(NPC npc, ref NPC.HitModifiers modifiers)
        {
            var config = ModContent.GetInstance<DDRConfig>();
            if (config == null || !config.EnableDamageReduction) return;
            if (!BossFightTracker.NpcTypeToFightKey.TryGetValue(npc.type, out string key)) return;
            if (!IsBossSelected(key, config)) return;

            var state = BossFightTracker.GetFightState(npc);
            if (state == null) return;

            float t2 = (Main.GameUpdateCount - state.StartTick) / 60f;
            float t1 = config.MinKillSeconds;
            if (t2 >= t1) return;

            bool isKillNpc = BossFightTracker.KillNpcTypes.Contains(npc.type);

            // 软层：SmoothedN 由 BossFightTracker.PostUpdateEverything 每帧维护
            modifiers.SourceDamage *= state.SmoothedN;

            if (isKillNpc)
            {
                // 致死 NPC 硬地板：全局 T2/T1，保证不早于 T1 死亡
                if (state.InitialHP.TryGetValue(npc.whoAmI, out int killInitHP) && killInitHP > 0)
                {
                    float hFloor = killInitHP * Math.Max(1f - t2 / t1, 0f);
                    modifiers.SetMaxDamage(Math.Max(1, npc.life - (int)hFloor));
                }
            }
            else if (state.HasNonKillParts && state.Phase2StartTick < 0)
            {
                // 非致死部位硬地板：仅在 Phase 1 期间，使用 α×T1 时间预算
                float T_phase1 = config.PhaseRatio * t1;
                if (T_phase1 > 0 &&
                    state.InitialHP.TryGetValue(npc.whoAmI, out int partInitHP) && partInitHP > 0)
                {
                    float hFloor = partInitHP * Math.Max(1f - t2 / T_phase1, 0f);
                    modifiers.SetMaxDamage(Math.Max(1, npc.life - (int)hFloor));
                }
            }
            // Phase 2 中的非致死部位（若仍存活）：仅受 SmoothedN 软约束，无硬地板
        }

        private static bool IsBossSelected(string key, DDRConfig cfg)
        {
            // ── 原版 Boss ─────────────────────────────────────────────
            bool vanilla = key switch
            {
                "KingSlime"      => cfg.KingSlime,
                "EyeOfCthulhu"   => cfg.EyeOfCthulhu,
                "EaterOfWorlds"  => cfg.EaterOfWorlds,
                "BrainOfCthulhu" => cfg.BrainOfCthulhu,
                "QueenBee"       => cfg.QueenBee,
                "Skeletron"      => cfg.Skeletron,
                "Deerclops"      => cfg.Deerclops,
                "WallOfFlesh"    => cfg.WallOfFlesh,
                "QueenSlime"     => cfg.QueenSlime,
                "TheTwins"       => cfg.TheTwins,
                "TheDestroyer"   => cfg.TheDestroyer,
                "SkeletronPrime" => cfg.SkeletronPrime,
                "Plantera"       => cfg.Plantera,
                "Golem"          => cfg.Golem,
                "DukeFishron"    => cfg.DukeFishron,
                "EmpressOfLight" => cfg.EmpressOfLight,
                "LunaticCultist" => cfg.LunaticCultist,
                "MoonLord"       => cfg.MoonLord,
                _                => false,
            };
            if (vanilla) return true;

            // ── 灾厄 Boss ────────────────────────────────────────────
            var cal = ModContent.GetInstance<DDRConfigCalamity>();
            if (cal != null)
            {
                bool calamity = key switch
                {
                "DesertScourge"        => cal.DesertScourge,
                "Crabulon"             => cal.Crabulon,
                "HiveMind"             => cal.HiveMind,
                "Perforators"          => cal.Perforators,
                "SlimeGod"             => cal.SlimeGod,
                "Cryogen"              => cal.Cryogen,
                "AquaticScourge"       => cal.AquaticScourge,
                "BrimstoneElemental"   => cal.BrimstoneElemental,
                "CalamitasClone"       => cal.CalamitasClone,
                "Leviathan"            => cal.Leviathan,
                "AstrumAureus"         => cal.AstrumAureus,
                "PlaguebringerGoliath" => cal.PlaguebringerGoliath,
                "Ravager"              => cal.Ravager,
                "AstrumDeus"           => cal.AstrumDeus,
                "ProfanedGuardians"    => cal.ProfanedGuardians,
                "Dragonfolly"          => cal.Dragonfolly,
                "Providence"           => cal.Providence,
                "StormWeaver"          => cal.StormWeaver,
                "CeaselessVoid"        => cal.CeaselessVoid,
                "Signus"               => cal.Signus,
                "Polterghast"          => cal.Polterghast,
                "OldDuke"              => cal.OldDuke,
                "DevourerofGods"       => cal.DevourerofGods,
                "Yharon"               => cal.Yharon,
                "ExoMechs"             => cal.ExoMechs,
                    "SupremeCalamitas"     => cal.SupremeCalamitas,
                    _                      => false,
                };
                if (calamity) return true;
            }

            // ── Fargo's Souls Mod Boss ───────────────────────────────
            var fargo = ModContent.GetInstance<DDRConfigFargo>();
            if (fargo == null) return false;

            return key switch
            {
                "TrojanSquirrel"  => fargo.TrojanSquirrel,
                "BanishedBaron"   => fargo.BanishedBaron,
                "DeviBoss"        => fargo.DeviBoss,
                "CursedCoffin"    => fargo.CursedCoffin,
                "Abomination"     => fargo.AbomBoss,
                "LifeChallenger"  => fargo.LifeChallenger,
                "MutantBoss"      => fargo.MutantBoss,
                "CosmosChampion"  => fargo.CosmosChampion,
                "EarthChampion"   => fargo.EarthChampion,
                "LifeChampion"    => fargo.LifeChampion,
                "NatureChampion"  => fargo.NatureChampion,
                "ShadowChampion"  => fargo.ShadowChampion,
                "SpiritChampion"  => fargo.SpiritChampion,
                "TerraChampion"   => fargo.TerraChampion,
                "TimberChampion"  => fargo.TimberChampion,
                "WillChampion"    => fargo.WillChampion,
                _                 => false,
            };
        }
    }
}
