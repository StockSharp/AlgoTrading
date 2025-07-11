# Adx Volume Strategy

Implementation of strategy #160 - ADX + Volume. Enter trades when ADX is
above threshold with above average volume. Direction determined by DI+
and DI- comparison.

High ADX denotes a strong trend and volume spikes confirm commitment. Entries are made when both indicators show strength together.

Great for catching energetic breakouts. A stop based on ATR keeps exposure in check.

## Details

- **Entry Criteria**:
  - Long: `ADX > AdxThreshold && Volume > AvgVolume`
  - Short: `ADX > AdxThreshold && Volume > AvgVolume`
- **Long/Short**: Both
- **Exit Criteria**: Trend weakens below threshold
- **Stops**: ATR-based using `StopLoss`
- **Default Values**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ADX, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
