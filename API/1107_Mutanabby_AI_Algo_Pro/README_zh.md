# Mutanabby AI Algo Pro
[English](README.md) | [Русский](README_ru.md)

该策略在出现看涨吞没形态、RSI 低于阈值且价格低于 N 根前的收盘价时做多。出现看跌吞没或触发止损时平仓。

## 细节
- **入场条件**：看涨吞没、稳定蜡烛、RSI 低于阈值、价格低于 N 根前。
- **方向**：仅做多。
- **出场条件**：看跌吞没或止损。
- **止损**：可选。
- **默认值**：
  - `CandleStabilityIndex` = 0.5
  - `RsiIndex` = 50
  - `CandleDeltaLength` = 5
  - `DisableRepeatingSignals` = false
  - `EnableStopLoss` = true
  - `StopLossMethod` = EntryPriceBased
  - `EntryStopLossPercent` = 2.0
  - `LookbackPeriod` = 10
  - `StopLossBufferPercent` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：趋势跟随
  - 方向：做多
  - 指标：RSI
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
