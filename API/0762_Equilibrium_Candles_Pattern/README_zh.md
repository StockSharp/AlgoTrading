# Equilibrium Candles Pattern 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用平衡蜡烛来识别短期趋势并在回调时入场。平衡线是指定周期内最高价与最低价的中点。经历一系列上涨或下跌蜡烛后，价格重新穿越平衡线触发交易。ATR 用于可选的止损/止盈，并在出现异常大的蜡烛时平仓。

## 细节

- **入场条件**：
  - **多头**：在上涨趋势后价格跌破平衡线。
  - **空头**：在下跌趋势后价格升破平衡线。
- **多空方向**：双向
- **止损**：基于 ATR 的止损和止盈（可选）
- **默认参数**：
  - `EquilibriumLength` = 9
  - `CandlesForTrend` = 7
  - `MaxPullbackCandles` = 2
  - `AtrPeriod` = 14
  - `StopMultiplier` = 2
  - `UseTpSl` = true
  - `UseBigCandleExit` = true
  - `BigCandleMultiplier` = 1
  - `UseReverse` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
