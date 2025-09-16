# 布林带突破
[English](README.md) | [Русский](README_ru.md)

基于原始 MQL 策略改写。该策略在布林带宽度超过阈值时，结合 EMA、MACD 和 RSI 的确认进行交易。每次波动性扩张只进场一次，止损沿中轨移动，并使用固定点数的止盈。

## 细节

- **入场条件**：
  - 多头：带宽超过 `BreakoutFactor`，MACD > 0，RSI > 50，EMA 高于中轨，上一根收盘价高于上一根上轨
  - 空头：带宽超过 `BreakoutFactor`，MACD < 0，RSI < 50，EMA 低于中轨，上一根收盘价低于上一根下轨
- **方向**：双向
- **出场条件**：
  - 多头：价格触及中轨止损或达到固定止盈
  - 空头：价格触及中轨止损或达到固定止盈
- **止损**：当前布林带中轨，每根K线更新
- **止盈**：按点数固定距离
- **默认值**：
  - `BollingerLength` = 18
  - `BollingerDeviation` = 2m
  - `BreakoutFactor` = 0.0015m
  - `TakeProfitPips` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：Breakout
  - 方向：双向
  - 指标：Bollinger Bands, EMA, MACD, RSI
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
