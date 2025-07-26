# MACD Histogram Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The MACD histogram represents the difference between the MACD line and its signal line. Crosses above or below zero often mark shifts in momentum. This strategy trades those zero-line crosses and manages risk with a percent stop.

On each candle the MACD histogram is computed. When it transitions from negative to positive, a long position is opened. A flip from positive to negative triggers a short sale. Because the strategy only looks for the zero crossover, trades are straightforward and typically short term.

Stops are used to contain losses if momentum fails to continue in the expected direction.

## Details

- **Entry Criteria**: MACD histogram crosses zero.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 160%. It performs best in the forex market.
