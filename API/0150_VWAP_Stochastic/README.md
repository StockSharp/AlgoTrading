# Vwap Stochastic Strategy

Strategy combining VWAP and Stochastic indicators. Buys when price is below VWAP and Stochastic is oversold. Sells when price is above VWAP and Stochastic is overbought.

VWAP marks the average trading level and Stochastic shows overbought or oversold conditions. Longs trigger below VWAP with a rising oscillator, shorts above VWAP with a falling one.

Day traders watching intraday value levels may benefit from this style. Stops are placed using an ATR multiple.

## Details

- **Entry Criteria**:
  - Long: `Close < VWAP && StochK < OversoldLevel`
  - Short: `Close > VWAP && StochK > OverboughtLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `Close > VWAP`
  - Short: `Close < VWAP`
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `OverboughtLevel` = 80m
  - `OversoldLevel` = 20m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: VWAP, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
