# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**BOSS动态减伤**（Dynamic Damage Reduction for Bosses）是一个 tModLoader 工具 Mod，供开发者测试 Boss 战斗时使用。核心功能：

- 通过 **ModConfig** 界面配置，无需物品
- 总开关 + 勾选具体 Boss（原版约 20 个，可扩展）
- 设置最短击杀时间（滑条 + 数字输入）
- 玩家第一次命中 Boss 时开始计时
- 动态计算每次伤害的减伤比例，使 Boss 血量下降曲线逼近最短击杀时间
- 多部位 Boss（月亮领主、毁灭者等）共用同一个计时器；各部位独立应用相同减伤公式
- 同时激活多个选中的 Boss 时各自独立计算

## Build

```
dotnet build DynamicDamageReductionforBosses.csproj
```

或在 tModLoader 游戏内 Workshop → Develop Mods → Build & Reload。无测试套件。

## Architecture

### 文件结构（计划）

```
DynamicDamageReductionforBosses.cs   ← 空 Mod 入口（当前已存在）
Config/
  DDRConfig.cs                        ← ModConfig：总开关、Boss 勾选列表、最短击杀时间
Systems/
  BossFightTracker.cs                 ← ModSystem：追踪各 Boss 战斗状态（计时器、初始HP等）
GlobalNPCs/
  DDRGlobalNPC.cs                     ← GlobalNPC：ModifyHitByProjectile / ModifyHitByItem 应用减伤
```

### 核心数据流

```
玩家命中 Boss
  → DDRGlobalNPC.ModifyHitBy*
    → 检查 DDRConfig.Enabled 和 Boss 是否被勾选
    → 查询 BossFightTracker 获取该 Boss 的计时和 HP 数据
    → 若尚未计时则开始计时（记录初始 HP）
    → 计算动态减伤系数并应用到 modifiers.SourceDamage
```

### 关键设计决策

- **多部位 Boss 共用计时器**：以"战斗ID"（如 `MoonLord`）作为 key，月亮领主三部位共享同一个 `BossFightState`
- **计时起点**：第一次命中时（`OnHitByProjectile` / `OnHitByItem`），不是 Boss 生成时
- **Boss 逃跑重置**：在 `BossFightTracker.PostUpdateEverything` 里检测 Boss 是否还活着，消失则清除战斗状态

### ModConfig 结构

- `bool Enabled` — 总开关
- `float MinKillSeconds` — 最短击杀时间（秒），滑条范围建议 10~600
- 每个 Boss 一个 `bool` 字段（按原版 Boss 分区列出）

### 命名约定

- 命名空间：`DynamicDamageReductionforBosses`
- 缩写前缀：`DDR`（用于类名避免歧义）
