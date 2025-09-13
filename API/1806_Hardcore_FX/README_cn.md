# Hardcore FX Breakout
[English](README.md) | [Русский](README_ru.md)

该策略改编自 MetaTrader 的 "HardcoreFX" 专家顾问。策略跟踪 ZigZag 的高点和低点，当价格突破这些水平时开仓。它使用固定的止损和止盈，并通过跟踪止损来保护已有利润。

## 细节
- **入场条件**：收盘价突破最近的 ZigZag 高点做多；收盘价跌破最近的 ZigZag 低点做空。
- **方向**：双向。
- **出场条件**：触发止损、止盈或跟踪止损。
- **止损**：固定止损、止盈和跟踪止损。
- **默认值**：
  - `ZigzagLength` = 17
  - `StopLoss` = 1400
  - `TakeProfit` = 5400
  - `TrailingStop` = 500
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤条件**：
  - 类别: 突破
  - 方向: 双向
  - 指标: Highest, Lowest
  - 止损: 止损、止盈、跟踪止损
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
