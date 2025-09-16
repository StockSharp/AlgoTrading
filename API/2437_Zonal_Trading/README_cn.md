# Zonal Trading
[English](README.md) | [Русский](README_ru.md)

Zonal Trading 策略复刻了 Bill Williams 的经典“区域”概念。它监控 Awesome Oscillator (AO) 与 Accelerator Oscillator (AC) 的柱状颜色。绿色表示当前值高于上一根柱，红色表示降低。当两个振荡器同时变绿时开多仓；当两者同时变红时开空仓。任一指标出现相反颜色时平掉现有仓位。

## 细节
- **入场条件**：
  - **多头**：AO 上升且 AC 上升。
  - **空头**：AO 下降且 AC 下降。
- **出场条件**：
  - **多头**：AO 或 AC 下降。
  - **空头**：AO 或 AC 上升。
- **止损**：默认不使用。
- **参数**：
  - `AoCandleType` – AO 使用的时间框架（默认 `H4`）。
  - `AcCandleType` – AC 使用的时间框架（默认 `H4`）。
  - `BuyOpen`, `SellOpen` – 启用/禁用多空开仓。
  - `BuyClose`, `SellClose` – 启用/禁用多空平仓。
- **指标**：Awesome Oscillator (5/34)、Accelerator Oscillator（AO 减去 SMA(5)）。
- **类型**：趋势动量型，适用于任何可获取上述指标的市场与周期。
