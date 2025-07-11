# Ma Cci Strategy

Strategy combining Moving Average and CCI indicators. Buys when price is
above MA and CCI is oversold. Sells when price is below MA and CCI is
overbought.

A moving average guides the trend while CCI looks for deviations from that average. Entries happen on CCI extremes in the direction of the MA.

Ideal for swing traders entering on pullbacks. ATR stops guard against sudden whipsaws.

## Details

- **Entry Criteria**:
  - Long: `Close > MA && CCI < OversoldLevel`
  - Short: `Close < MA && CCI > OverboughtLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - CCI returns to zero line
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `MaPeriod` = 20
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Moving Average, CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
