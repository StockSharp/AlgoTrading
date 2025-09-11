# Momentum Alligator 4h Bitcoin 策略
[English](README.md) | [Русский](README_ru.md)

Momentum Alligator 4h Bitcoin 策略将 Awesome 振荡器与日线级别的 Alligator 指标结合使用。当振荡器上穿其 5 周期 SMA 且价格位于日线 Alligator 三条线之上时开多。动态止损在入场价按百分比下移和 Alligator 下颚线之间取较高值。盈利平仓后策略会跳过接下来的两个信号。

## 细节

- **入场条件**：AO 上穿其 5 周期 SMA 且收盘价高于日线 Alligator 各线。
- **多空方向**：仅做多。
- **出场条件**：动态止损，取百分比止损与 Alligator 下颚的较大者。
- **止损**：有。
- **默认值**：
  - `StopLossPercent` = 0.02m
  - `CandleType` = TimeSpan.FromHours(4)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **过滤器**：
  - 类别：Momentum
  - 方向：Long
  - 指标：Awesome Oscillator, Alligator
  - 止损：有
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
