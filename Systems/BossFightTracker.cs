using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DynamicDamageReductionforBosses.Systems
{
    /// <summary>
    /// 记录各 Boss 战斗的计时器和初始 HP。
    ///
    /// 设计要点：
    ///  - 以"战斗 Key"（如 "MoonLord"）而非 NPC 类型或 whoAmI 为单位追踪。
    ///  - 多部位 Boss 的所有部位共享同一个 BossFightState（同一 Key）。
    ///  - 每个 NPC 部位第一次被命中时记录其初始 HP（使用 whoAmI 区分）。
    ///  - PostUpdateEverything 每帧检测：若某 Key 下所有 NPC 都消失，则清除该战斗状态。
    /// </summary>
    public class BossFightTracker : ModSystem
    {
        // ── 战斗状态 ─────────────────────────────────────────────────

        public class BossFightState
        {
            /// <summary>战斗第一次命中时的 Main.GameUpdateCount。</summary>
            public int StartTick;

            /// <summary>whoAmI → 该 NPC 被首次命中时的 HP。</summary>
            public readonly Dictionary<int, int> InitialHP = new();
        }

        /// <summary>fightKey → 战斗状态。</summary>
        public static readonly Dictionary<string, BossFightState> ActiveFights = new();

        // ── NPC 类型 → 战斗 Key 映射 ─────────────────────────────────
        // 同一 Boss 的所有部位映射到相同 Key，共享计时器。

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
        /// 在 Boss 生成时调用（OnSpawn）。
        /// 若该 Key 下尚无战斗状态，则创建并开始计时。
        /// 记录初始 HP = lifeMax（生成时满血）。
        /// 多部位 Boss 的各部位各自调用，共享同一个 StartTick。
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

            // 记录该部位的初始 HP（生成时为满血，用 lifeMax 更准确）
            if (!state.InitialHP.ContainsKey(npc.whoAmI))
                state.InitialHP[npc.whoAmI] = npc.lifeMax;
        }

        /// <summary>
        /// 在 NPC 被命中前调用（ModifyHitBy*），作为安全兜底。
        /// 正常情况下生成时已注册；此处处理 OnSpawn 未能覆盖的边缘情况
        /// （如世界吞噬怪分裂出的新段落）。
        /// </summary>
        public static BossFightState GetFightState(NPC npc)
        {
            if (!NpcTypeToFightKey.TryGetValue(npc.type, out string key))
                return null;

            if (!ActiveFights.TryGetValue(key, out var state))
            {
                // 兜底：OnSpawn 被错过（例如 Config 在 Boss 已存在时才启用）
                state = new BossFightState { StartTick = (int)Main.GameUpdateCount };
                ActiveFights[key] = state;
            }

            // 兜底：若该部位未被记录初始 HP，用当前 HP 补录
            if (!state.InitialHP.ContainsKey(npc.whoAmI))
                state.InitialHP[npc.whoAmI] = npc.life;

            return state;
        }

        /// <summary>每帧清理已结束的战斗（所有相关 NPC 均不活跃）。</summary>
        public override void PostUpdateEverything()
        {
            if (ActiveFights.Count == 0) return;

            var toRemove = new List<string>();

            foreach (var (key, state) in ActiveFights)
            {
                bool anyAlive = false;
                foreach (int whoAmI in state.InitialHP.Keys)
                {
                    if (whoAmI < 0 || whoAmI >= Main.maxNPCs) continue;
                    NPC npc = Main.npc[whoAmI];
                    if (npc.active && NpcTypeToFightKey.ContainsKey(npc.type))
                    {
                        anyAlive = true;
                        break;
                    }
                }
                if (!anyAlive)
                    toRemove.Add(key);
            }

            foreach (string key in toRemove)
                ActiveFights.Remove(key);
        }

        public override void OnWorldUnload()
        {
            ActiveFights.Clear();
        }
    }
}
