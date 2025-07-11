# Gann Swing Breakout

Strategy based on Gann Swing Breakout technique

Gann Swing Breakout tracks swing highs and lows from Gann analysis. A breakout beyond the latest swing starts a trade in that direction and it stays open until an opposing swing is breached.

The method is designed for traders who view past swing points as important support and resistance. By trading the break, it attempts to ride the next leg of a trend.


## Details

- **Entry Criteria**: Signals based on MA, Gann.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `SwingLookback` = 5
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: MA, Gann
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
