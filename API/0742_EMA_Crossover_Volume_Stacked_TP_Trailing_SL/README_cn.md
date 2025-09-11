# EMA交叉成交量+分批止盈与跟踪止损策略
[English](README.md) | [Русский](README_ru.md)

该策略在成交量放大的情况下交易EMA交叉。入场后设置两个基于ATR的分批止盈，并在价格有利移动后启动跟踪止损。

## 细节

- **入场条件**：
  - 快速EMA上穿/下穿慢速EMA。
  - 成交量 > 平均成交量 * `VolumeMultiplier`。
- **多空方向**：多头和空头。
- **出场条件**：
  - 第一目标 `TP1Multiplier * ATR`（平仓33%）。
  - 第二目标 `TP2Multiplier * ATR`（再平仓33%）。
  - 当价格移动 `TrailTriggerMultiplier * ATR` 后启用跟踪止损，距离为 `TrailOffsetMultiplier * ATR`。
- **止损**：仅跟踪止损。
- **默认值**：
  - `FastLength` = 21
  - `SlowLength` = 55
  - `VolumeMultiplier` = 1.2
  - `AtrLength` = 14
  - `Tp1Multiplier` = 1.5
  - `Tp2Multiplier` = 2.5
  - `TrailOffsetMultiplier` = 1.5
  - `TrailTriggerMultiplier` = 1.5
  - `CandleType` = 5m
- **过滤器**：
  - 类型：趋势跟随
  - 方向：多空
  - 指标：EMA, ATR, Volume
  - 止损：有
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险：中等
