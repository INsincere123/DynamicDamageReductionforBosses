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
        /// <summary>
        /// 实际生效时间 = MinKillSeconds - EndBuffer。
        /// 让底线提前消失，避免在最后一帧卡着触发 CheckDead。
        /// </summary>
        private const float EndBuffer = 1f;     // 1 秒缓冲
        // ── Boss 生成 ─────────────────────────────────────────────────
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            var config = ModContent.GetInstance<DDRConfig>();
            if (config == null) return;
            if (!BossFightTracker.NpcTypeToFightKey.TryGetValue(npc.type, out string key)) return;
            if (!IsBossSelected(key, config)) return;

            // 血量倍数（独立开关，先于计时器注册执行，确保 initialHP 记录缩放后的值）
            if (config.EnableHpMultiplier)
            {
                int multiplier = Math.Max(1, Math.Min(2000, config.HpMultiplier));
                if (multiplier > 1)
                {
                    npc.lifeMax = (int)(npc.lifeMax * multiplier);
                    npc.life    = npc.lifeMax;
                }
            }

            // 动态减伤需要记录计时器和初始 HP
            if (config.EnableDamageReduction)
                BossFightTracker.RegisterSpawn(npc);
        }

        // ── 层1：软减伤 ───────────────────────────────────────────────
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
            => ApplySoftReduction(npc, ref modifiers);

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
            => ApplySoftReduction(npc, ref modifiers);

        // ── 层2：HP 底线兜底 ──────────────────────────────────────────
        public override bool CheckDead(NPC npc)
        {
            var config = ModContent.GetInstance<DDRConfig>();
            if (config == null || !config.EnableDamageReduction) return true;
            if (!BossFightTracker.NpcTypeToFightKey.TryGetValue(npc.type, out string key)) return true;
            if (!IsBossSelected(key, config)) return true;

            var state = BossFightTracker.GetFightState(npc);
            if (state == null || !state.InitialHP.TryGetValue(npc.whoAmI, out int initialHP)) return true;

            float elapsed        = (Main.GameUpdateCount - state.StartTick) / 60f;
            float effectiveTime  = MathF.Max(1f, config.MinKillSeconds) - EndBuffer;
            if (elapsed >= effectiveTime) return true;

            int floor = SuperellipseFloor(initialHP, elapsed / effectiveTime, MathF.Max(0.1f, config.CurvePower));
            if (floor <= 0) return true;

            npc.life = floor;
            return false;
        }

        // ─────────────────────────────────────────────────────────────

        private static void ApplySoftReduction(NPC npc, ref NPC.HitModifiers modifiers)
        {
            var config = ModContent.GetInstance<DDRConfig>();
            if (config == null || !config.EnableDamageReduction) return;
            if (!BossFightTracker.NpcTypeToFightKey.TryGetValue(npc.type, out string key)) return;
            if (!IsBossSelected(key, config)) return;

            var state = BossFightTracker.GetFightState(npc);
            if (state == null || !state.InitialHP.TryGetValue(npc.whoAmI, out int initialHP)) return;
            if (initialHP <= 0) return;

            float elapsed       = (Main.GameUpdateCount - state.StartTick) / 60f;
            float effectiveTime = MathF.Max(1f, config.MinKillSeconds) - EndBuffer;
            if (elapsed >= effectiveTime) return;

            float p      = MathF.Max(0.1f, config.CurvePower);
            float tRatio = elapsed / effectiveTime;
            float hRatio = (float)npc.life / initialHP;

            float S     = MathF.Pow(tRatio, p) + MathF.Pow(hRatio, p);
            float delta = MathF.Max(0f, 1f - S);
            if (delta > 0f)
            {
                float multiplier = 1f / (1f + 10f * delta);
                modifiers.SourceDamage *= MathF.Max(0.01f, multiplier);
            }

            int floor = SuperellipseFloor(initialHP, tRatio, p);
            if (floor > 0 && npc.life > floor)
                modifiers.SetMaxDamage(Math.Max(1, npc.life - floor));
        }

        private static int SuperellipseFloor(int initialHP, float tRatio, float p)
        {
            float inner = 1f - MathF.Pow(tRatio, p);
            if (inner <= 0f) return 0;
            return (int)(initialHP * MathF.Pow(inner, 1f / p));
        }

        private static bool IsBossSelected(string key, DDRConfig cfg) => key switch
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
    }
}
