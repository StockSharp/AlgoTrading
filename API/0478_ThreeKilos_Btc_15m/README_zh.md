# Three Kilos BTC 15m 策略
[English](README.md) | [Русский](README_ru.md)

Three Kilos BTC 15m 策略结合三条 TEMA 指标和 Supertrend 滤波器。当中期 TEMA 上穿短期 TEMA、同时高于长期 TEMA 且 Supertrend 显示上升趋势时开多；当短期 TEMA 上穿中期 TEMA、同时低于长期 TEMA 且 Supertrend 显示下降趋势时开空。固定百分比的止盈和止损用于风险控制。

## 详情

- **入场条件**:
  - **多头**: TEMA2 上穿 TEMA1，TEMA2 > TEMA3，Supertrend 上升趋势。
  - **空头**: TEMA1 上穿 TEMA2，TEMA2 < TEMA3，Supertrend 下降趋势。
- **方向**: 双向。
- **出场条件**:
  - 止盈或止损。
- **止损**: 止盈 1%，止损 1%。
- **默认参数**:
  - `ShortPeriod` = 30
  - `LongPeriod` = 50
  - `Long2Period` = 140
  - `AtrLength` = 10
  - `Multiplier` = 2
  - `TakeProfit` = 1%
  - `StopLoss` = 1%
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 双向
  - 指标: TEMA, Supertrend, ATR
  - 止损: 止盈和止损
  - 复杂度: 中等
  - 时间框架: 15m
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
