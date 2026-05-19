# BOSS动态减伤 / Dynamic Boss Damage Reduction

> 装备太强，打 Boss 太快？这个 Mod 帮你找回完整的战斗体验  
> Too strong for your own good? This mod gives you back the fight

---

## 中文

### 这是什么

带着后期装备打前期 Boss，结果一开始就结束了——这种感觉你懂的。

本 Mod 能让你重新好好打一场 Boss 战。不管装备多强，Boss 都会撑够你设定的时间，血量曲线平滑自然，不会卡住，也不会打出满屏的 1。时间一到，限制立刻解除，痛快收尾。

对于**模组开发者和武器测试者**：高 DPS 武器在前期 Boss 身上根本无法正常测试。本 Mod 能强制 Boss 存活足够时间，让你完整收集战斗数据。配合血量倍数还可以模拟更长的持续输出，测量 DPS 曲线、触发频率等关键指标。

### 使用方法

1. 启用 Mod，进入游戏
2. 打开 **设置 → Mod 配置**，找到对应配置页
3. 勾选需要限制的 Boss，设置最短击杀时间
4. 进入游戏正常战斗即可，无需任何物品

### 配置参数

| 参数 | 说明 |
|------|------|
| 启用动态减伤 | 总开关 |
| 最短击杀时间（秒） | Boss 至少存活多少秒，超时后伤害完全恢复 |
| 减伤强度 | 值越大，DPS 超标时减伤越猛烈（默认 2.0） |
| 最低伤害比例 | 最低保留的伤害比例，防止打出满屏 1（默认 5%） |
| 附属部位时间占比 | 多段 Boss 中，手/臂等附属部位占用时间的比例（默认 50%） |
| 启用血量倍数 | 开启后，Boss 生成时血量乘以指定倍数 |

### 支持的模组

| 配置页 | 覆盖范围 |
|--------|---------|
| ① 原版 Boss | 所有 20 个原版 Boss |
| ② 灾厄 Boss | Calamity Mod 全部 Boss |
| ③ Fargo's Souls | Fargo's Souls Mod 主线 Boss + 9 位英灵 |
| ④ 瑟银 Mod | Thorium Mod 全部 Boss（含迷你 Boss） |
| ⑤ 旅人归途 Boss | Homeward Journey Boss |
| ⑥ 自定义 Boss | 手动填写，支持任意模组 |

②③④⑤ 为可选依赖，未安装时对应配置项自动无效。

### 自定义 Boss

对于上表未覆盖的模组，可在 **⑥ 自定义 Boss** 页手动添加条目：

| 字段 | 说明 | 示例 |
|------|------|------|
| 模组内部名 | 模组的技术名称（非显示名） | `CalamityMod` |
| NPC 类名 | Boss NPC 的 C# 类名 | `SupremeCalamitas` |
| 分组名 | 多段 Boss 的共享计时器标识，单体留空 |  |
| 致死 NPC | 勾选 = 受硬地板保护；不勾 = 只受软减伤（适用于手/臂等附属部位） | ✓ |

进入世界时，聊天框会打印每条记录的注册结果（✓ 成功 / ✗ 失败及原因）。在游戏中修改设置保存后也会立即重新注册并打印结果。

### 技术说明

**减伤原理（双层结构）**

- **软层**：每帧根据 Boss 当前血量与理想进度的偏差，用双曲正切函数平滑更新减伤系数，血量下降自然流畅
- **硬地板**：基于线性血量下限，防止极端爆发伤害穿透软层，致死 NPC 绝对不会早于最短击杀时间死亡

**多段 Boss 两段式计时**

含附属部位的 Boss（月亮领主、石巨人等）分两个阶段：
- 第一阶段（前 α×T1 秒）：手/臂等非致死部位受保护
- 第二阶段（后 (1-α)×T1 秒）：本体/核心受保护
- 相位切换（如月亮领主手的脚本死亡）不被拦截

**已知限制**

- 世界吞噬者体节死亡会触发分裂，无法对其使用硬地板；软减伤仍然生效
- 本 Mod 为单机测试工具，联机行为未经测试

---

## English

### What is this

You go back to fight an early boss with your endgame gear — and it's over before the music even kicks in.

This mod gives you back the fight. No matter how powerful you are, the boss will last as long as you set it to. HP drains smoothly and naturally, with no stalling and no walls of 1s. When the timer runs out, all limits lift instantly — finish it off.

For **mod developers and weapon testers**: high-DPS weapons are nearly impossible to test on early bosses — they just instantly die. This mod keeps any boss alive long enough to run the full fight, no matter how broken your damage is. Combine it with the HP multiplier to simulate extended encounters and measure DPS curves, proc rates, and fight pacing.

### How to use

1. Enable the mod and launch the game
2. Go to **Settings → Mod Configuration** and find the config pages
3. Check the bosses you want to limit and set the minimum kill time
4. Play normally — no items or special setup required

### Configuration

| Option | Description |
|--------|-------------|
| Enable Damage Reduction | Master toggle |
| Minimum Kill Time (s) | How long the boss must survive; damage fully restores after the timer |
| Reduction Strength | Higher = stronger reduction when DPS exceeds target (default 2.0) |
| Minimum Damage Ratio | Damage floor — each hit always deals at least this fraction of base damage (default 5%) |
| Limb Phase Ratio | For multi-part bosses: fraction of the timer reserved for killing limbs (default 50%) |
| Enable HP Multiplier | Multiply a boss's max HP on spawn |

### Supported mods

| Config page | Coverage |
|-------------|---------|
| ① Vanilla Bosses | All 20 vanilla bosses |
| ② Calamity Bosses | Full Calamity Mod boss roster |
| ③ Fargo's Souls | Fargo's Souls Mod main bosses + 9 champions |
| ④ Thorium Mod | All Thorium bosses including mini-bosses |
| ⑤ Homeward Journey Bosses | Homeward Journey boss roster |
| ⑥ Custom Bosses | Any mod — fill in manually |

②③④⑤ are optional. If not installed, their config entries have no effect.

### Custom bosses

For mods not listed above, use the **⑥ Custom Bosses** config page to add entries manually:

| Field | Description | Example |
|-------|-------------|---------|
| Mod Internal Name | The mod's technical name (not its display name) | `CalamityMod` |
| NPC Class Name | The C# class name of the boss NPC | `SupremeCalamitas` |
| Group Key | Shared timer key for multi-part bosses; leave empty for standalone bosses |  |
| Is Kill NPC | Checked = HP floor protection; unchecked = soft reduction only (use for limbs/appendages) | ✓ |

When entering a world, the chat will print a per-entry result (✓ registered / ✗ failed with reason). Saving changes in-game triggers an immediate re-registration and new output.

### Technical notes

**Two-layer reduction system**

- **Soft layer**: Every frame, a smooth multiplier (`SmoothedN`) is updated via a tanh-based curve comparing current HP to the ideal drain schedule. This gives fluid, natural HP loss.
- **Hard floor**: A linear HP floor prevents extreme burst damage from bypassing the soft layer. Kill-condition NPCs are guaranteed not to die before the minimum time.

**Two-phase timer for multi-part bosses**

Bosses with non-kill appendages (Moon Lord, Golem, etc.) use a two-phase system:
- **Phase 1** (first α×T1 seconds): Limbs/appendages are protected
- **Phase 2** (final (1-α)×T1 seconds): The main body/core is protected
- Scripted phase transitions (e.g., Moon Lord hands dying) are not blocked

**Known limitations**

- Eater of Worlds segments trigger splitting on death; hard floors are not applied to them (would freeze the fight). Soft reduction still applies.
- This mod is designed for single-player testing. Multiplayer behavior is untested.

---

## Build

```
dotnet build DynamicDamageReductionforBosses.csproj
```

Or use **Workshop → Develop Mods → Build & Reload** in-game.

## License

This project is open source. Feel free to use the damage reduction logic in your own mods with credit.
