# MACD RSI EMA BB ATR 日内交易策略
[English](README.md) | [Русский](README_ru.md)

该策略适用于日内交易，结合MACD信号交叉、RSI区间和EMA趋势方向，并使用布林带挤压过滤。风险管理基于ATR止损、ATR跟踪止损以及风险收益比的止盈。

## 详情

- **入场条件**：MACD按趋势方向穿越信号，RSI处于阈值范围内，且没有BB挤压。
- **多空方向**：双向。
- **出场条件**：相反的止损或目标。
- **止损**：基于ATR的止损、跟踪止损和RiskReward止盈。
- **默认参数**：
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `EmaFast` = 9
  - `EmaSlow` = 21
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `TrailAtrMultiplier` = 1.5
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：Trend Following
  - 方向：双向
  - 指标：MACD, RSI, EMA, Bollinger Bands, ATR
  - 止损：是
  - 复杂度：中等
  - 周期：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
