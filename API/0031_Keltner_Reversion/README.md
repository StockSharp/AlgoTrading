# Keltner Reversion

Strategy that trades on mean reversion using Keltner Channels

Keltner Reversion fades pushes outside the Keltner Channel. Entries bet on a return toward the middle band, closing trades once price re-enters the channel or the stop is hit.

The channel width expands and contracts with volatility, allowing the system to catch extreme moves while giving trades room to develop. Stops are typically based on ATR multiples.


## Details

- **Entry Criteria**: Signals based on RSI, ATR, Keltner.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: RSI, ATR, Keltner
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
