# VWAP 策略

使用 VWAP 及其入场通道并提供多种退出模式。当价格收于下方通道上方时做多，收于上方通道下方时做空。支持按 VWAP 或偏差通道退出，并可在连续反向柱后触发安全退出。

## 参数

- **StopPoints**：信号柱高/低点缓冲。
- **ExitModeLong**：多头退出模式。
- **ExitModeShort**：空头退出模式。
- **TargetLongDeviation**：多头目标偏差倍数。
- **TargetShortDeviation**：空头目标偏差倍数。
- **EnableSafetyExit**：启用安全退出。
- **NumOpposingBars**：安全退出所需的反向柱数。
- **AllowLongs**：允许多头。
- **AllowShorts**：允许空头。
- **MinStrength**：最小信号强度。
- **CandleType**：蜡烛类型。

