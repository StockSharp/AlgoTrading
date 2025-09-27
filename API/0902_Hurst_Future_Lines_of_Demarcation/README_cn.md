# Hurst Future Lines of Demarcation 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用平滑的 FLD 以及三个周期（信号、交易、趋势）。当价格在特定趋势状态下突破信号 FLD 时入场，并在选定数值发生交叉时退出。

## 详情

- **入场条件**:
  - 当价格上穿信号 FLD 且趋势状态为 1 时买入。
  - 当价格下破信号 FLD 且趋势状态为 6 时卖出。
- **方向**: 多/空。
- **出场条件**: 当 `CloseTrigger1` 与 `CloseTrigger2` 交叉且方向与当前仓位相反时平仓。
- **止损**: 无。
- **默认参数**:
  - `SmoothFld` = false
  - `FldSmoothing` = 5
  - `SignalCycleLength` = 5
  - `TradeCycleLength` = 20
  - `TrendCycleLength` = 80
  - `CloseTrigger1` = Price
  - `CloseTrigger2` = Trade
