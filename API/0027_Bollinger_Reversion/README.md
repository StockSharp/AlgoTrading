# Bollinger Reversion

Strategy based on Bollinger Bands mean reversion

Bollinger Reversion fades moves outside the Bollinger Bands. Trades open against closes beyond the bands and close once price returns inside or hits a stop.

Standard deviation bands offer a statistical view of overextension. Entering after extreme closes aims to profit from the snap back toward the middle band.


## Details

- **Entry Criteria**: Signals based on RSI, ATR, Bollinger.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: RSI, ATR, Bollinger
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
