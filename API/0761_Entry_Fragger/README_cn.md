# Entry Fragger 策略
[English](README.md) | [Русский](README_ru.md)

该策略跟踪相对于50周期EMA的红色和绿色蜡烛序列。在EMA下方出现一系列红色蜡烛后，收盘价高于波动云的绿色蜡烛触发做多信号。类似地，绿色蜡烛序列后出现的红色蜡烛用于做空。可选的反向交易模式允许在信号反转时直接翻转仓位。

## 细节

- **入场条件**：
  - **做多**：`redCount >= Buy Signal Accuracy` 且最后一根红色蜡烛在EMA50下方，当前绿色蜡烛收盘价高于 `EMA50 + stdev/4`。
  - **做空**：`greenCount >= Sell Signal Accuracy` 且前一根蜡烛为绿色，当前红色蜡烛收盘价高于 `EMA50 + stdev/4`。
- **多/空**：双向。
- **出场条件**：反向信号。
- **指标**：EMA、StandardDeviation。
- **默认值**：
  - `Buy Signal Accuracy` = 2
  - `Sell Signal Accuracy` = 2
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：多个
  - 止损：无
  - 复杂度：中等
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
