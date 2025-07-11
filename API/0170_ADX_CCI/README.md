# Adx Cci Strategy

Strategy based on ADX and CCI indicators. Enters long when ADX > 25 and
CCI is oversold (< -100) Enters short when ADX > 25 and CCI is
overbought (> 100)

ADX assesses whether a trend has strength and CCI identifies entry timing after pullbacks. Longs and shorts follow the ADX direction.

Geared toward momentum traders entering on retracements. ATR multiples manage risk.

## Details

- **Entry Criteria**:
  - Long: `ADX > 25 && CCI < -100`
  - Short: `ADX > 25 && CCI > 100`
- **Long/Short**: Both
- **Exit Criteria**: Trend weakens or CCI crosses zero
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `AdxPeriod` = 14
  - `CciPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: ADX, CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
