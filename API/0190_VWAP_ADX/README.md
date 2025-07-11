# Vwap Adx Strategy

Strategy based on VWAP and ADX indicators. Enters long when price is
above VWAP and ADX > 25. Enters short when price is below VWAP and ADX >
25. Exits when ADX < 20.

VWAP acts as the session benchmark, and ADX measures conviction. Entries appear when price departs from VWAP with ADX showing strength.

Fits intraday trend traders. Protective stops use ATR multiples.

## Details

- **Entry Criteria**:
  - Long: `Close > VWAP && ADX > 25`
  - Short: `Close < VWAP && ADX > 25`
- **Long/Short**: Both
- **Exit Criteria**: ADX drops below threshold
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `StopLossPercent` = 2m
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: VWAP, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
