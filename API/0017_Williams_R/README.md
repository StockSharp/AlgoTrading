# Williams R

Strategy based on Williams %R indicator

Williams %R identifies overbought and oversold zones. When the indicator rises above the upper threshold it signals potential weakness for shorts; readings below the lower threshold suggest longs. Positions close once %R moves toward neutral.

Because %R oscillates quickly, the strategy can generate many signals in volatile markets. Some traders combine it with other filters to reduce noise.


## Details

- **Entry Criteria**: Signals based on Williams.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `Period` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Williams
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
