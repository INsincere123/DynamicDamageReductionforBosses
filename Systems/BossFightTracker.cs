using System;
using System.Collections.Generic;
using DynamicDamageReductionforBosses.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DynamicDamageReductionforBosses.Systems
{
    /// <summary>
    /// 追踪各 Boss 战斗状态，实现两段式时间预算控制。
    ///
    /// 设计要点：
    ///  - 以战斗 Key 为单位；多部位 Boss 共享同一 BossFightState。
    ///  - 区分"致死 NPC"（击杀=战斗结束）和"非致死部位"（手/臂/拳头等）。
    ///  - Phase 1：非致死部位受 α×T1 时间预算保护；SmoothedN 基于非致死部位血量。
    ///  - Phase 2：致死 NPC 受 (1-α)×T1 时间预算保护；SmoothedN 基于致死 NPC 血量。
    ///  - 单体 Boss（无非致死部位）：跳过 Phase 1，Phase 2 使用完整 T1。
    /// </summary>
    public class BossFightTracker : ModSystem
    {
        // ── 战斗状态 ─────────────────────────────────────────────────

        public class BossFightState
        {
            /// <summary>战斗开始时的 Main.GameUpdateCount。</summary>
            public int StartTick;

            /// <summary>whoAmI → 该 NPC 首次记录时的 HP（RegisterSpawn 或兜底）。</summary>
            public readonly Dictionary<int, int> InitialHP = new();

            /// <summary>非致死部位初始 HP 之和（仅 RegisterSpawn 时累加）。</summary>
            public int NonKillTotalInitHP;

            /// <summary>致死 NPC 初始 HP 之和（仅 RegisterSpawn 时累加）。</summary>
            public int KillTotalInitHP;

            /// <summary>是否存在非致死部位（决定是否启用两段制）。</summary>
            public bool HasNonKillParts;

            /// <summary>Phase 2 开始时的 GameUpdateCount；-1 表示 Phase 1 尚未结束。</summary>
            public int Phase2StartTick = -1;

            /// <summary>每帧更新的平滑减伤系数，由 PostUpdateEverything 维护。</summary>
            public float SmoothedN = 1f;
        }

        /// <summary>fightKey → 战斗状态。</summary>
        public static readonly Dictionary<string, BossFightState> ActiveFights = new();

        // ── 致死 NPC 集合 ─────────────────────────────────────────────
        // 只有这些 NPC 的死亡才等同于"Boss 被击败"。
        // 硬地板（SetMaxDamage + CheckDead）仅作用于此集合内的 NPC；
        // 其余部位只受 SmoothedN 约束，可以自然死亡（脚本触发、相位切换等）。

        public static readonly HashSet<int> KillNpcTypes = new()
        {
            // ── 困难模式前 ───────────────────────────────────────────
            NPCID.KingSlime,
            NPCID.EyeofCthulhu,
            // EaterOfWorlds 不列入：头死=分裂，拦截会卡死战斗
            NPCID.BrainofCthulhu,
            NPCID.QueenBee,
            NPCID.SkeletronHead,       // 骷髅头死亡=战斗结束；双手可以自然死亡
            NPCID.Deerclops,
            NPCID.WallofFlesh,
            NPCID.WallofFleshEye,      // WoF 与眼共享击杀条件
            // ── 困难模式 ────────────────────────────────────────────
            NPCID.QueenSlimeBoss,
            NPCID.Retinazer,           // 双子都是致死 NPC，两者均需死亡
            NPCID.Spazmatism,
            NPCID.TheDestroyer,        // 毁灭者头；体节可自然死亡
            NPCID.SkeletronPrime,      // 机械骷髅头；四条手臂可自然死亡
            NPCID.Plantera,
            NPCID.Golem,               // 石巨人体；头/拳头可自然死亡
            NPCID.DukeFishron,
            NPCID.HallowBoss,
            NPCID.CultistBoss,
            NPCID.MoonLordCore,        // 月亮领主核心；手/头由脚本触发死亡，不拦截
        };

        // ── NPC 类型 → 战斗 Key 映射 ─────────────────────────────────

        public static readonly Dictionary<int, string> NpcTypeToFightKey = new()
        {
            // 困难模式前
            { NPCID.KingSlime,            "KingSlime"       },
            { NPCID.EyeofCthulhu,         "EyeOfCthulhu"    },
            { NPCID.EaterofWorldsHead,     "EaterOfWorlds"   },
            { NPCID.EaterofWorldsBody,     "EaterOfWorlds"   },
            { NPCID.EaterofWorldsTail,     "EaterOfWorlds"   },
            { NPCID.BrainofCthulhu,        "BrainOfCthulhu"  },
            { NPCID.QueenBee,              "QueenBee"        },
            { NPCID.SkeletronHead,         "Skeletron"       },
            { NPCID.SkeletronHand,         "Skeletron"       },
            { NPCID.Deerclops,             "Deerclops"       },
            { NPCID.WallofFlesh,           "WallOfFlesh"     },
            { NPCID.WallofFleshEye,        "WallOfFlesh"     },
            // 困难模式
            { NPCID.QueenSlimeBoss,        "QueenSlime"      },
            { NPCID.Retinazer,             "TheTwins"        },
            { NPCID.Spazmatism,            "TheTwins"        },
            { NPCID.TheDestroyer,          "TheDestroyer"    },
            { NPCID.TheDestroyerBody,      "TheDestroyer"    },
            { NPCID.SkeletronPrime,        "SkeletronPrime"  },
            { NPCID.PrimeCannon,           "SkeletronPrime"  },
            { NPCID.PrimeSaw,              "SkeletronPrime"  },
            { NPCID.PrimeVice,             "SkeletronPrime"  },
            { NPCID.PrimeLaser,            "SkeletronPrime"  },
            { NPCID.Plantera,              "Plantera"        },
            { NPCID.Golem,                 "Golem"           },
            { NPCID.GolemHead,             "Golem"           },
            { NPCID.GolemHeadFree,         "Golem"           },
            { NPCID.GolemFistLeft,         "Golem"           },
            { NPCID.GolemFistRight,        "Golem"           },
            { NPCID.DukeFishron,           "DukeFishron"     },
            { NPCID.HallowBoss,            "EmpressOfLight"  },
            { NPCID.CultistBoss,           "LunaticCultist"  },
            { NPCID.MoonLordHead,          "MoonLord"        },
            { NPCID.MoonLordHand,          "MoonLord"        },
            { NPCID.MoonLordCore,          "MoonLord"        },
        };

        // ── 公开方法 ─────────────────────────────────────────────────

        /// <summary>
        /// Boss 生成时调用。分别累加非致死和致死 NPC 的初始 HP。
        /// 多部位 Boss 各部位各自调用，共享同一 StartTick。
        /// </summary>
        public static void RegisterSpawn(NPC npc)
        {
            if (!NpcTypeToFightKey.TryGetValue(npc.type, out string key))
                return;

            if (!ActiveFights.TryGetValue(key, out var state))
            {
                state = new BossFightState { StartTick = (int)Main.GameUpdateCount };
                ActiveFights[key] = state;
            }

            if (!state.InitialHP.ContainsKey(npc.whoAmI))
            {
                state.InitialHP[npc.whoAmI] = npc.lifeMax;

                if (KillNpcTypes.Contains(npc.type))
                    state.KillTotalInitHP += npc.lifeMax;
                else
                {
                    state.NonKillTotalInitHP += npc.lifeMax;
                    state.HasNonKillParts = true;
                }
            }
        }

        /// <summary>
        /// 命中时获取战斗状态，兼作兜底注册（分裂段、Config 晚于生成启用等边缘情况）。
        /// 兜底注册不改动 HP 预算字段，避免统计失真。
        /// </summary>
        public static BossFightState GetFightState(NPC npc)
        {
            if (!NpcTypeToFightKey.TryGetValue(npc.type, out string key))
                return null;

            if (!ActiveFights.TryGetValue(key, out var state))
            {
                state = new BossFightState { StartTick = (int)Main.GameUpdateCount };
                ActiveFights[key] = state;
            }

            if (!state.InitialHP.ContainsKey(npc.whoAmI))
                state.InitialHP[npc.whoAmI] = npc.life;

            return state;
        }

        // ── 每帧更新 ─────────────────────────────────────────────────

        public override void PostUpdateEverything()
        {
            if (ActiveFights.Count == 0) return;

            var config = ModContent.GetInstance<DDRConfigVanilla>();
            var toRemove = new List<string>();
            const float dt = 1f / 60f;

            foreach (var (key, state) in ActiveFights)
            {
                bool anyAlive = false;
                int nonKillCurrentHP = 0;
                int killCurrentHP = 0;
                bool anyNonKillAlive = false;

                foreach (var (whoAmI, _) in state.InitialHP)
                {
                    if (whoAmI < 0 || whoAmI >= Main.maxNPCs) continue;
                    NPC npc = Main.npc[whoAmI];
                    if (!npc.active || !NpcTypeToFightKey.ContainsKey(npc.type)) continue;

                    anyAlive = true;
                    if (KillNpcTypes.Contains(npc.type))
                        killCurrentHP += npc.life;
                    else
                    {
                        nonKillCurrentHP += npc.life;
                        anyNonKillAlive = true;
                    }
                }

                if (!anyAlive)
                {
                    toRemove.Add(key);
                    continue;
                }

                if (config == null || !config.EnableDamageReduction)
                {
                    state.SmoothedN = 1f;
                    continue;
                }

                float t2 = (Main.GameUpdateCount - state.StartTick) / 60f;
                float t1 = config.MinKillSeconds;
                float alpha = config.PhaseRatio;

                // Phase 2 启动检测
                if (state.Phase2StartTick < 0)
                {
                    // 单体/全致死 Boss 立即进入 Phase 2；有非致死部位的等待触发条件
                    bool phase1Over = !state.HasNonKillParts
                                   || !anyNonKillAlive
                                   || t2 >= alpha * t1;
                    if (phase1Over)
                        state.Phase2StartTick = (int)Main.GameUpdateCount;
                }

                if (state.Phase2StartTick < 0)
                {
                    // ── Phase 1：基于非致死部位 HP 和 α×T1 时间预算 ─────────
                    float T_phase1 = Math.Max(alpha * t1, 0.1f);
                    float h = state.NonKillTotalInitHP > 0
                        ? (float)nonKillCurrentHP / state.NonKillTotalInitHP
                        : 0f;
                    float q = Math.Max((T_phase1 - t2) / T_phase1, 0f);
                    UpdateSmoothedN(ref state.SmoothedN, h, q, config, dt);
                }
                else
                {
                    // ── Phase 2：基于致死 NPC 血量和剩余时间预算 ──────────────
                    // 单体 Boss（!HasNonKillParts）使用完整 T1；
                    // 有非致死部位的 Boss 使用 (1-α)×T1。
                    float phase2Duration = state.HasNonKillParts ? (1f - alpha) * t1 : t1;
                    float phase2Elapsed  = (Main.GameUpdateCount - state.Phase2StartTick) / 60f;

                    if (phase2Elapsed >= phase2Duration || t2 >= t1)
                    {
                        state.SmoothedN = 1f;
                    }
                    else
                    {
                        float h = state.KillTotalInitHP > 0
                            ? (float)killCurrentHP / state.KillTotalInitHP
                            : 0f;
                        float q = (phase2Duration - phase2Elapsed) / phase2Duration;
                        UpdateSmoothedN(ref state.SmoothedN, h, q, config, dt);
                    }
                }
            }

            foreach (string key in toRemove)
                ActiveFights.Remove(key);
        }

        private static void UpdateSmoothedN(ref float smoothedN, float h, float q, DDRConfigVanilla config, float dt)
        {
            float deficit    = q > 0f ? Math.Max(0f, (q - h) / q) : 0f;
            float timeFactor = 0.35f + 0.65f * (1f - q);
            float danger     = MathF.Tanh(config.Sensitivity * deficit) * timeFactor;
            float minN       = config.MinDamageRatio;
            float targetN    = Math.Clamp(1f - (1f - minN) * danger, minN, 1f);
            float tau        = targetN < smoothedN ? 0.12f : 0.35f;
            float lerpFactor = 1f - MathF.Exp(-dt / tau);
            smoothedN        = smoothedN + (targetN - smoothedN) * lerpFactor;
            smoothedN        = Math.Clamp(smoothedN, minN, 1f);
        }

        public override void PostSetupContent()
        {
            RegisterCalamityBosses();
            RegisterFargoBosses();
            RegisterThoriumBosses();
        }

        /// <summary>
        /// 在 PostSetupContent 里动态注册灾厄 Boss 的 NPC 类型映射。
        /// 灾厄 NPC 的 Type ID 是运行时确定的，不能用常量，必须在此处查询。
        /// </summary>
        private static void RegisterCalamityBosses()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out _)) return;

            void TryAdd(string className, string key)
            {
                if (ModContent.TryFind<ModNPC>($"CalamityMod/{className}", out var npc))
                    NpcTypeToFightKey[npc.Type] = key;
            }

            // ── 困难模式前 ───────────────────────────────────────────
            TryAdd("DesertScourgeHead",        "DesertScourge");
            TryAdd("DesertScourgeBody",        "DesertScourge");
            TryAdd("DesertScourgeTail",        "DesertScourge");
            TryAdd("Crabulon",                 "Crabulon");
            TryAdd("HiveMind",                 "HiveMind");
            TryAdd("PerforatorHive",           "Perforators");
            TryAdd("PerforatorHeadLarge",      "Perforators");
            TryAdd("PerforatorHeadMedium",     "Perforators");
            TryAdd("PerforatorHeadSmall",      "Perforators");
            TryAdd("SlimeGodCore",             "SlimeGod");
            TryAdd("CrimulanPaladin",          "SlimeGod");
            TryAdd("EbonianPaladin",           "SlimeGod");
            TryAdd("SplitCrimulanPaladin",     "SlimeGod");
            TryAdd("SplitEbonianPaladin",      "SlimeGod");

            // ── 困难模式 ─────────────────────────────────────────────
            TryAdd("Cryogen",                  "Cryogen");
            TryAdd("AquaticScourgeHead",       "AquaticScourge");
            TryAdd("AquaticScourgeBody",       "AquaticScourge");
            TryAdd("AquaticScourgeBodyAlt",    "AquaticScourge");
            TryAdd("AquaticScourgeTail",       "AquaticScourge");
            TryAdd("BrimstoneElemental",       "BrimstoneElemental");
            TryAdd("CalamitasClone",           "CalamitasClone");
            TryAdd("Leviathan",                "Leviathan");
            TryAdd("Anahita",                  "Leviathan");
            TryAdd("AstrumAureus",             "AstrumAureus");
            TryAdd("PlaguebringerGoliath",     "PlaguebringerGoliath");
            TryAdd("RavagerHead",              "Ravager");
            TryAdd("AstrumDeusHead",           "AstrumDeus");
            TryAdd("AstrumDeusBody",           "AstrumDeus");
            TryAdd("AstrumDeusTail",           "AstrumDeus");

            // ── 月亮领主后 ───────────────────────────────────────────
            TryAdd("ProfanedGuardianCommander","ProfanedGuardians");
            TryAdd("ProfanedGuardianDefender", "ProfanedGuardians");
            TryAdd("ProfanedGuardianHealer",   "ProfanedGuardians");
            TryAdd("Dragonfolly",              "Dragonfolly");
            TryAdd("Providence",               "Providence");
            TryAdd("StormWeaverHead",          "StormWeaver");
            TryAdd("CeaselessVoid",            "CeaselessVoid");
            TryAdd("Signus",                   "Signus");
            TryAdd("Polterghast",              "Polterghast");
            TryAdd("OldDuke",                  "OldDuke");
            TryAdd("DevourerofGodsHead",       "DevourerofGods");
            TryAdd("DevourerofGodsBody",       "DevourerofGods");
            TryAdd("DevourerofGodsTail",       "DevourerofGods");
            TryAdd("Yharon",                   "Yharon");
            TryAdd("AresBody",                 "ExoMechs");
            TryAdd("ThanatosHead",             "ExoMechs");
            TryAdd("ThanatosBody1",            "ExoMechs");
            TryAdd("ThanatosBody2",            "ExoMechs");
            TryAdd("ThanatosTail",             "ExoMechs");
            TryAdd("Apollo",                   "ExoMechs");
            TryAdd("Artemis",                  "ExoMechs");
            TryAdd("SupremeCalamitas",         "SupremeCalamitas");

            // ── 灾厄致死 NPC 注册 ────────────────────────────────────
            void TryAddKill(string className)
            {
                if (ModContent.TryFind<ModNPC>($"CalamityMod/{className}", out var npc))
                    KillNpcTypes.Add(npc.Type);
            }

            // 困难模式前
            TryAddKill("DesertScourgeHead");
            TryAddKill("Crabulon");
            TryAddKill("HiveMind");
            TryAddKill("PerforatorHive");
            TryAddKill("SlimeGodCore");
            // 困难模式
            TryAddKill("Cryogen");
            TryAddKill("AquaticScourgeHead");
            TryAddKill("BrimstoneElemental");
            TryAddKill("CalamitasClone");
            TryAddKill("Leviathan");
            TryAddKill("Anahita");
            TryAddKill("AstrumAureus");
            TryAddKill("PlaguebringerGoliath");
            TryAddKill("RavagerHead");
            TryAddKill("AstrumDeusHead");
            // 月亮领主后
            TryAddKill("ProfanedGuardianCommander");
            TryAddKill("ProfanedGuardianDefender");
            TryAddKill("ProfanedGuardianHealer");
            TryAddKill("Dragonfolly");
            TryAddKill("Providence");
            TryAddKill("StormWeaverHead");
            TryAddKill("CeaselessVoid");
            TryAddKill("Signus");
            TryAddKill("Polterghast");
            TryAddKill("OldDuke");
            TryAddKill("DevourerofGodsHead");
            TryAddKill("Yharon");
            TryAddKill("AresBody");
            TryAddKill("Apollo");
            TryAddKill("Artemis");
            TryAddKill("ThanatosHead");
            TryAddKill("SupremeCalamitas");
        }

        /// <summary>
        /// 在 PostSetupContent 里动态注册 Fargo's Souls Mod Boss 的 NPC 类型映射。
        /// </summary>
        private static void RegisterFargoBosses()
        {
            if (!ModLoader.TryGetMod("FargowiltasSouls", out _)) return;

            void TryAdd(string className, string key)
            {
                if (ModContent.TryFind<ModNPC>($"FargowiltasSouls/{className}", out var npc))
                    NpcTypeToFightKey[npc.Type] = key;
            }

            void TryAddKill(string className)
            {
                if (ModContent.TryFind<ModNPC>($"FargowiltasSouls/{className}", out var npc))
                    KillNpcTypes.Add(npc.Type);
            }

            // ── 主线 Boss ────────────────────────────────────────────
            TryAdd("TrojanSquirrel",      "TrojanSquirrel");
            TryAdd("TrojanSquirrelHead",  "TrojanSquirrel");
            TryAdd("TrojanSquirrelArms",  "TrojanSquirrel");
            TryAddKill("TrojanSquirrel");                      // 躯干死亡=战斗结束；头/手臂可自然死亡

            TryAdd("BanishedBaron",       "BanishedBaron");
            TryAddKill("BanishedBaron");

            TryAdd("DeviBoss",            "DeviBoss");
            TryAddKill("DeviBoss");

            TryAdd("CursedCoffin",        "CursedCoffin");
            TryAdd("CursedSpirit",        "CursedCoffin");
            TryAddKill("CursedCoffin");                        // CursedSpirit 可自然死亡

            TryAdd("AbomBoss",            "Abomination");
            TryAdd("AbomSaucer",          "Abomination");
            TryAddKill("AbomBoss");                            // AbomSaucer 可自然死亡

            TryAdd("LifeChallenger",      "LifeChallenger");
            TryAddKill("LifeChallenger");

            TryAdd("MutantBoss",          "MutantBoss");
            TryAdd("MutantIllusion",      "MutantBoss");
            TryAddKill("MutantBoss");                          // MutantIllusion 可自然死亡

            // ── 冠军 Boss ────────────────────────────────────────────
            TryAdd("CosmosChampion",      "CosmosChampion");
            TryAddKill("CosmosChampion");

            TryAdd("EarthChampion",       "EarthChampion");
            TryAdd("EarthChampionHand",   "EarthChampion");
            TryAddKill("EarthChampion");

            TryAdd("LifeChampion",        "LifeChampion");
            TryAddKill("LifeChampion");

            TryAdd("NatureChampion",      "NatureChampion");
            TryAdd("NatureChampionHead",  "NatureChampion");
            TryAddKill("NatureChampion");

            TryAdd("ShadowChampion",      "ShadowChampion");
            TryAdd("ShadowOrbNPC",        "ShadowChampion");
            TryAddKill("ShadowChampion");

            TryAdd("SpiritChampion",      "SpiritChampion");
            TryAdd("SpiritChampionHand",  "SpiritChampion");
            TryAddKill("SpiritChampion");

            TryAdd("TerraChampion",       "TerraChampion");
            TryAdd("TerraChampionBody",   "TerraChampion");
            TryAddKill("TerraChampion");

            TryAdd("TimberChampionHead",  "TimberChampion");  // Head 是主体
            TryAdd("TimberChampion",      "TimberChampion");
            TryAddKill("TimberChampionHead");

            TryAdd("WillChampion",        "WillChampion");
            TryAddKill("WillChampion");
        }

        /// <summary>
        /// 在 PostSetupContent 里动态注册 Thorium Mod Boss 的 NPC 类型映射。
        /// </summary>
        private static void RegisterThoriumBosses()
        {
            if (!ModLoader.TryGetMod("ThoriumMod", out _)) return;

            void TryAdd(string className, string key)
            {
                if (ModContent.TryFind<ModNPC>($"ThoriumMod/{className}", out var npc))
                    NpcTypeToFightKey[npc.Type] = key;
            }

            void TryAddKill(string className)
            {
                if (ModContent.TryFind<ModNPC>($"ThoriumMod/{className}", out var npc))
                    KillNpcTypes.Add(npc.Type);
            }

            // ── 困难模式前 ───────────────────────────────────────────
            TryAdd("Viscount",               "Viscount");
            TryAdd("BiteyBaby",              "Viscount");
            TryAddKill("Viscount");

            TryAdd("GraniteEnergyStorm",     "GraniteEnergyStorm");
            TryAdd("CoalescedEnergy",        "GraniteEnergyStorm");
            TryAdd("EncroachingEnergy",      "GraniteEnergyStorm");
            TryAdd("EnergyBarrier",          "GraniteEnergyStorm");
            TryAdd("EnergyConduit",          "GraniteEnergyStorm");
            TryAdd("UnstableEnergyAnomaly",  "GraniteEnergyStorm");
            TryAddKill("GraniteEnergyStorm");

            TryAdd("Illusionist",            "Illusionist");
            TryAdd("IllusionistDecoy",       "Illusionist");
            TryAdd("IllusionGlass",          "Illusionist");
            TryAddKill("Illusionist");                         // IllusionistDecoy 可自然死亡

            TryAdd("PatchWerk",              "PatchWerk");
            TryAddKill("PatchWerk");

            TryAdd("QueenJellyfish",         "QueenJellyfish");
            TryAdd("DistractingJellyfish",   "QueenJellyfish");
            TryAdd("SpittingJellyfish",      "QueenJellyfish");
            TryAdd("ZealousJellyfish",       "QueenJellyfish");
            TryAddKill("QueenJellyfish");

            TryAdd("TheGrandThunderBird",    "TheGrandThunderBird");
            TryAdd("StormHatchling",         "TheGrandThunderBird");
            TryAddKill("TheGrandThunderBird");

            // ── 困难模式 ─────────────────────────────────────────────
            // BoreanStrider：主体 CheckDead 设 life=1 变形为 Popped，Popped 才是致死 NPC
            TryAdd("BoreanStrider",          "BoreanStrider");
            TryAdd("BoreanStriderPopped",    "BoreanStrider");
            TryAdd("BoreanHopper",           "BoreanStrider");
            TryAdd("BoreanMyte",             "BoreanStrider");
            TryAddKill("BoreanStriderPopped");                 // 主体 transforms，不列入

            // FallenBeholder：普通→FallenBeholder 终态，专家→FallenBeholder2 终态
            TryAdd("FallenBeholder",         "FallenBeholder");
            TryAdd("FallenBeholder2",        "FallenBeholder");
            TryAdd("Beholder",               "FallenBeholder");
            TryAddKill("FallenBeholder");
            TryAddKill("FallenBeholder2");

            TryAdd("BuriedChampion",         "BuriedChampion");
            TryAdd("FallenChampion1",        "BuriedChampion");
            TryAdd("FallenChampion2",        "BuriedChampion");
            TryAdd("BizarreRockFormation",   "BuriedChampion");
            TryAdd("MagicalBurst",           "BuriedChampion");
            TryAddKill("BuriedChampion");

            TryAdd("CorpseBloom",            "CorpseBloom");
            TryAdd("CorpsePetal",            "CorpseBloom");
            TryAdd("CorpseWeed",             "CorpseBloom");
            TryAdd("BurstingMaggot",         "CorpseBloom");
            TryAdd("FamishedMaggot",         "CorpseBloom");
            TryAddKill("CorpseBloom");

            // Lich：三相（Lich→LichHeadless→PhylacteryofaThousandSouls），各相 OnKill 均有终结逻辑
            TryAdd("Lich",                         "Lich");
            TryAdd("LichHeadless",                 "Lich");
            TryAdd("PhylacteryofaThousandSouls",   "Lich");
            TryAddKill("Lich");
            TryAddKill("LichHeadless");
            TryAddKill("PhylacteryofaThousandSouls");

            TryAdd("StarScouter",            "StarScouter");
            TryAdd("BioCore",                "StarScouter");
            TryAdd("CryoCore",               "StarScouter");
            TryAdd("PyroCore",               "StarScouter");
            TryAddKill("StarScouter");

            // ForgottenOne：三相连锁（ForgottenOne→Cracked→Released）
            // ForgottenOne / ForgottenOneCracked 的 CheckDead 设 life=1 触发下一相，不拦截
            TryAdd("ForgottenOne",           "ForgottenOne");
            TryAdd("ForgottenOneCracked",    "ForgottenOne");
            TryAdd("ForgottenOneReleased",   "ForgottenOne");
            TryAdd("AbyssalSpawn",           "ForgottenOne");
            TryAddKill("ForgottenOneReleased");                // 前两相 transforms，只有 Released 是致死 NPC

            // ── 月亮领主后：四原初 ────────────────────────────────────
            // Aquaius/Omnicide/SlagFury 死亡时 PrimordialBase.OnKill 触发 DreamEater 召唤
            // DreamEater 是战斗的最终终结者，为唯一致死 NPC
            TryAdd("Aquaius",                "ThePrimordials");
            TryAdd("AquaiusBubble",          "ThePrimordials");
            TryAdd("Omnicide",               "ThePrimordials");
            TryAdd("SlagFury",               "ThePrimordials");
            TryAdd("DreamEater",             "ThePrimordials");
            TryAdd("ImpendingDread",         "ThePrimordials");
            TryAdd("InnerDespair",           "ThePrimordials");
            TryAdd("UnstableAnger",          "ThePrimordials");
            TryAdd("LucidBubble",            "ThePrimordials");
            TryAddKill("DreamEater");
        }

        public override void OnWorldUnload()
        {
            ActiveFights.Clear();
        }
    }
}
