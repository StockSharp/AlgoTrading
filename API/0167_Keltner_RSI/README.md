# Keltner Rsi Strategy

Strategy combining Keltner Channels and RSI indicators. Looks for mean reversion opportunities when price touches channel boundaries and RSI confirms oversold/overbought conditions.

Keltner Channels map recent volatility while RSI measures momentum extremes. Entries occur when RSI supports a move beyond the channel.

Great for bounce traders around volatility envelopes. Stops rely on an ATR multiplier.

## Details

- **Entry Criteria**:
  - Long: `Close < LowerBand && RSI < RsiOversoldLevel`
  - Short: `Close > UpperBand && RSI > RsiOverboughtLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - Price returns to EMA
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Keltner Channel, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
