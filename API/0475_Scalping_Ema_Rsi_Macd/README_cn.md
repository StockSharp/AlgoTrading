# Scalping EMA RSI MACD 策略
[English](README.md) | [Русский](README_ru.md)

该策略在30分钟K线下运行，结合快慢EMA交叉、趋势EMA、RSI与MACD过滤，并加入成交量条件。止损基于ATR，止盈使用固定的风险收益比。

## 详情

- **入场条件**：EMA交叉符合趋势方向，RSI位于指定区间，MACD确认且成交量放大。
- **多空方向**：双向。
- **出场条件**：止损或止盈触发。
- **止损**：基于ATR的止损与RiskReward止盈。
- **默认参数**：
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
  - `TrendEmaLength` = 55
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `VolumeMaLength` = 20
  - `VolumeThreshold` = 1.3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **过滤器**：
  - 类别：Scalping
  - 方向：双向
  - 指标：EMA, RSI, MACD, ATR, Volume
  - 止损：是
  - 复杂度：中等
  - 周期：日内 (30m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
