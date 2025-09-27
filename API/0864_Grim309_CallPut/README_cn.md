# GRIM309 CallPut Strategy
[English](README.md) | [Русский](README_ru.md)

GRIM309 CallPut Strategy 基于多重 EMA 趋势对齐并带有预警机制。EMA10 高于 EMA20 且价格高于 EMA50 时，EMA5 上升并突破 EMA10 开多单；相反条件下开空单。平仓后有冷却期防止立即再次开仓。当 EMA5 与 EMA10 之间的差值迅速收缩时，预警会提前平仓。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **做多**：EMA10 > EMA20，价格 > EMA50，EMA5 上升且 > EMA10，无持仓并满足冷却期。
  - **做空**：EMA10 < EMA20，价格 < EMA50，EMA5 下降且 < EMA10，无持仓并满足冷却期。
- **出场条件**：价格穿越 EMA15 或触发预警。
- **止损**：无。
- **默认值**：
  - `Ema5Length` = 5
  - `Ema10Length` = 10
  - `Ema15Length` = 15
  - `Ema20Length` = 20
  - `Ema50Length` = 50
  - `Ema200Length` = 200
  - `CooldownBars` = 2
- **筛选**：
  - 分类：趋势跟随
  - 方向：多 & 空
  - 指标：EMA
  - 复杂度：中等
  - 风险等级：中等
