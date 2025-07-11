# Parabolic Sar Rsi Strategy

Strategy that combines Parabolic SAR for trend direction and RSI for
entry confirmation with oversold/overbought conditions.

Here the Parabolic SAR outlines the prevailing trend and RSI measures exhaustion. Trades are opened once both indicators signal the same direction.

The combination is appealing to those who like trailing stops, since SAR also provides a dynamic exit. The stop placement follows the SAR curve.

## Details

- **Entry Criteria**:
  - Long: `Close > SAR && RSI < RsiOversold`
  - Short: `Close < SAR && RSI > RsiOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `Close < SAR`
  - Short: `Close > SAR`
- **Stops**: Uses Parabolic SAR as a trailing stop
- **Default Values**:
  - `SarAf` = 0.02m
  - `SarMaxAf` = 0.2m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Parabolic SAR, Parabolic SAR, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
