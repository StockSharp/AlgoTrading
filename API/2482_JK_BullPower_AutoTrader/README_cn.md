# JK BullP AutoTrader
[English](README.md) | [Русский](README_ru.md)

JK BullP AutoTrader 是原始 MetaTrader 智能交易系统的移植版本，依赖 Bulls Power 振荡指标。策略比较连续两个 Bulls Power 值：当两根柱子都在零线上方且最新一根低于更早一根时，视为多头动能减弱并做空；当上一根 Bulls Power 跌破零线时做多。多空仓位均使用固定止盈、固定止损以及随盈利逐步收紧的移动止损进行保护。

## 详情

- **入场条件**：当前一根 Bulls Power 高于零且两根前的值更高时做空；当上一根 Bulls Power 低于零时做多。
- **多空方向**：双向。
- **出场条件**：触发固定止盈、固定止损或移动止损；出现相反信号时反手。
- **止损类型**：固定止盈、固定止损、移动止损。
- **默认参数**：
  - `BullsPeriod` = 13
  - `TakeProfitPoints` = 350
  - `StopLossPoints` = 100
  - `TrailingStopPoints` = 100
  - `TrailingStepPoints` = 40
  - `CandleType` = TimeSpan.FromHours(1)
- **筛选**：
  - 类别：振荡指标
  - 方向：双向
  - 指标：Bulls Power
  - 止损：固定 + 移动
  - 复杂度：基础
  - 周期：日内/波段（1 小时）
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
