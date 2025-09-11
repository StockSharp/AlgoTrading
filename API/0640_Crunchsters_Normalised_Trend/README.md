# Crunchsters Normalised Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that normalizes returns and applies a Hull Moving Average to the cumulative normalized price.
Enters long when the normalized price crosses above the HMA and short when it crosses below.

Testing indicates an average annual return of about 105%. It performs best in the crypto market.

Normalized returns allow price to be scaled by recent volatility. An ATR-based stop manages risk.

## Details

- **Entry Criteria**:
  - Long: `nPrice` crosses above `HMA`
  - Short: `nPrice` crosses below `HMA`
- **Long/Short**: Both
- **Exit Criteria**: Opposite crossover or ATR stop
- **Stops**: ATR-based using `StopMultiple`
- **Default Values**:
  - `NormPeriod` = 14
  - `HmaPeriod` = 100
  - `HmaOffset` = 0
  - `StopMultiple` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Hull Moving Average, Standard Deviation, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
