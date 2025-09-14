# Fibo Candles Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the custom **Fibo Candles** technique to determine trend direction.
The indicator paints each candle in one of two colors based on a Fibonacci ratio comparison
between the current close and the recent high/low range. A switch in color signals a potential
reversal. When the color turns bullish the strategy closes any short position and opens a long
position. When the color turns bearish it closes any long position and opens a short.

The method adapts to market volatility through a lookback period and selectable Fibonacci level.
A stop loss and take profit in absolute points protect every trade.

## Details

- **Entry Criteria**:
  - **Long**: Current candle color changes from bearish to bullish.
  - **Short**: Current candle color changes from bullish to bearish.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Existing positions are closed when the opposite color appears.
- **Stops**: Fixed stop loss and take profit in points via `StartProtection`.
- **Default Values**:
  - `Period` = 10 (candles used to measure high/low range).
  - `Fibo Level` = 0.236 (ratio used for trend decision).
  - `Stop Loss` = 1000 points.
  - `Take Profit` = 2000 points.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Hourly by default
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate

