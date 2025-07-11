# RSI Reversion

Strategy based on RSI mean reversion

RSI Reversion assumes price will revert after reaching extreme RSI values. When RSI falls below the lower threshold it buys; when above the upper threshold it sells. Positions close as RSI moves back toward neutral.

The extremes can be calibrated to suit various markets. Using additional filters like trend direction helps avoid fading strong moves too early.


## Details

- **Entry Criteria**: Signals based on RSI.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `OversoldThreshold` = 30m
  - `OverboughtThreshold` = 70m
  - `ExitLevel` = 50m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
