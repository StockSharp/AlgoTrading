# Fibonacci Retracement Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Markets often retrace a portion of a prior move before resuming trend. This strategy identifies recent swing highs and lows and watches for price to test the 61.8% or 78.6% retracement levels. These areas frequently mark exhaustion points.

Testing indicates an average annual return of about 115%. It performs best in the stocks market.

The algorithm tracks swings over a rolling window and calculates Fibonacci levels between them. When price nears a key retracement and forms a candle in the direction of the original trend, a trade is opened with a stop placed a set percent away. Targets are around the 50% midpoint of the swing.

By focusing on deep pullbacks within an existing trend, the method aims to capture the early stages of a continuation move after sellers or buyers have briefly taken control.

## Details

- **Entry Criteria**: Price tests 61.8% or 78.6% retracement and prints a confirming candle.
- **Long/Short**: Both depending on trend.
- **Exit Criteria**: Price reaching the 50% level or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `SwingLookbackPeriod` = 20
  - `FibLevelBuffer` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Fibonacci levels
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

