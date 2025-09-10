# Arsi Vwap Atr 策略
[English](README.md) | [Русский](README_ru.md)

自适应 RSI 策略，超买和超卖水平根据 ATR 或偏离 VWAP 动态调整。当 RSI 穿越这些自适应水平时开仓，当 RSI 回到中间区域时平仓。

## 细节

- **入场条件**：
  - 多头：`RSI` 上穿自适应超卖线
  - 空头：`RSI` 下穿自适应超买线
- **多空**：双向
- **出场条件**：
  - RSI 再次穿越 50 或相反的自适应线
- **止损**：基于百分比，使用 `StopLossPercent` 和 `RiskReward`
- **默认参数**：
  - `RsiLength` = 14
  - `BaseK` = 1m
  - `RiskPercent` = 2m
  - `StopLossPercent` = 2.5m
  - `RiskReward` = 2m
  - `SourceOb` = ATR
  - `SourceOs` = ATR
  - `AtrLengthOb` = 14
  - `AtrLengthOs` = 14
  - `ObMultiplier` = 10m
  - `OsMultiplier` = 10m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：RSI、ATR、VWAP
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
