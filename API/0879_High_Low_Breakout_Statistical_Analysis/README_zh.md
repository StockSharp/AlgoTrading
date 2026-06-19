# High Low Breakout Statistical Analysis 策略
[English](README.md) | [Русский](README_ru.md)

基于所选时间框架的高点或低点突破进行交易。根据参数决定做多或做空，并在固定条数后平仓。

## 详情

- **入场条件**：收盘价突破所选高点或低点。
- **多空方向**：双向。
- **退出条件**：反向信号或持有 HoldingPeriod 条后。
- **止损**：否。
- **默认值**：
  - `EntryOption` = LongAtHigh
  - `TimeframeOption` = Daily
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类型: 突破
  - 方向: 双向
  - 指标: 高点, 低点
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
