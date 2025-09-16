# Exp Multic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-currency strategy that trades a fixed set of major Forex pairs without technical indicators.
For each pair the algorithm maintains a direction and volume. After each profitable move the volume is increased, after a loss the direction is flipped. Trading stops and all positions are closed once overall profit or loss exceeds specified thresholds.

## Details

- **Entry Criteria**:
  - If no position and account balance above `Margin`, open position in predefined direction with `MinVolume`.
- **Long/Short**: Both, depending on internal direction per pair.
- **Exit Criteria**:
  - Close position when profit exceeds `KClose * MinVolume`.
  - Reverse direction and close when loss exceeds `KChange * current volume`.
- **Stops**: No explicit stops; risk controlled by profit/loss thresholds.
- **Default Values**:
  - `Loss` = 1900
  - `Profit` = 4000
  - `Margin` = 5000
  - `MinVolume` = 0.01
  - `KChange` = 2100
  - `KClose` = 4600
- **Filters**:
  - Category: Money management
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Tick-based
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
