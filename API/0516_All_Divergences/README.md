# All Divergences Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The All Divergences strategy looks for bullish and bearish RSI divergences filtered by a moving average trend. A long position is opened when price makes a lower low while RSI forms a higher low above the moving average. A short position is opened when price makes a higher high while RSI forms a lower high below the moving average. Optional stop-loss and take-profit protection can automatically close positions, and a moving average risk control exits after several closes against the trend.

## Details

- **Entry Criteria**:
  - Price relative to moving average defines trend.
  - **Long**: price makes lower low, RSI higher low, price above MA.
  - **Short**: price makes higher high, RSI lower high, price below MA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal or MA risk exit.
- **Stops**: Optional stop-loss and take-profit.
- **Default Values**:
  - `MaLength` = 50
  - `RsiLength` = 14
  - `MaRiskCandles` = 3
  - `UseProtection` = False
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: RSI, Moving Average
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
