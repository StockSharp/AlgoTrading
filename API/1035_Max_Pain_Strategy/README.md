# Max Pain Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long positions when both volume and price movement exceed configurable thresholds while the VIX index remains below a specified level. A volatility-based stop-loss is set on entry and the position is closed after a fixed number of periods.

## Details

- **Entry Criteria**:
  - **Long**: volume greater than average volume × `VolumeMultiplier` and price change greater than previous close × `PriceChangeMultiplier` with VIX below `VixThreshold`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Stop-loss at `StopLossMultiplier` × volatility below entry price.
  - Close position after `HoldPeriods` bars.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackPeriod` = 70.
  - `VolumeMultiplier` = 1.
  - `PriceChangeMultiplier` = 0.029.
  - `StopLossMultiplier` = 2.4.
  - `VixThreshold` = 44.
  - `HoldPeriods` = 8.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
  - `VixCandleType` = TimeSpan.FromDays(1).TimeFrame().
- **Filters**:
  - Category: Breakout
  - Direction: Long only
  - Indicators: Volume, price action, volatility
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
