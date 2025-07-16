# Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This statistical approach looks for short-term extremes in price relative to its recent average. The strategy uses a moving average to define fair value and measures the deviation from that mean through a standard deviation calculation.

Trades are opened when price pushes a set distance from the average. A dip below the lower band triggers a long entry, anticipating a rebound toward the mean, while a rally above the upper band prompts a short. Once price touches the moving average again, any open position is closed.

The method appeals to traders who prefer a contrarian style and want clearly defined entry and exit zones. Because it relies on volatility-based bands, it adapts to quieter or more active markets while still keeping losses in check via a fixed stop-loss.

## Details
- **Entry Criteria**:
  - **Long**: Price < MA - k*StdDev (below lower band)
  - **Short**: Price > MA + k*StdDev (above upper band)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when price crosses above the moving average
  - **Short**: Exit when price crosses below the moving average
- **Stops**: Yes.
- **Default Values**:
  - `MovingAveragePeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Mean Reversion
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
