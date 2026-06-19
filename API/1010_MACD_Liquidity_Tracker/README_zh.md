# MACD Liquidity Tracker
[English](README.md) | [Русский](README_ru.md)

MACD Liquidity Tracker 利用 MACD 的颜色状态生成交易信号。提供 Fast、Normal、Safe、Crossover 四种模式来调节信号灵敏度，并可选用止损和止盈。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：取决于 `SystemType`（默认 `Normal` 为 MACD 在线上方）。
  - **空头**：取决于 `SystemType`（默认 `Normal` 为 MACD 在线下方）。
- **出场条件**：反向信号。
- **止损**：可选的止损与止盈。
- **默认值**：
  - `FastLength` = 25
  - `SlowLength` = 60
  - `SignalLength` = 220
  - `AllowShortTrades` = false
  - `SystemType` = Normal
  - `UseStopLoss` = false
  - `StopLossPercent` = 3
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 6
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = tf(5)
- **筛选**：
  - 分类：趋势
  - 方向：多头与空头
  - 指标：MACD
  - 止损：有
  - 复杂度：基础
  - 周期：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
