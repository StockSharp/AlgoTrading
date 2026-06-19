# Hammer + EMA 策略（基于 tick 的止损/止盈）
[English](README.md) | [Русский](README_ru.md)

结合锤子和倒锤子形态，与 EMA 趋势过滤和基于 tick 的风险管理。

## 详情
- **入场条件**: EMA 上方的锤子或 EMA 下方的倒锤子。
- **多空方向**: 双向。
- **退出条件**: 基于 tick 的止盈或止损。
- **止损**: 基于 tick。
- **默认值**:
  - `EmaLength` = 50
  - `StopLossTicks` = 1
  - `TakeProfitTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 形态
  - 方向: 双向
  - 指标: EMA, 锤子, 倒锤子
  - 止损: 基于 tick
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
