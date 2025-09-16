# Bollinger Band Two MA ZigZag Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Hybrid trend-following system that combines Bollinger Band reversals, two higher-timeframe moving averages, and swing points from a ZigZag detector. It opens two positions on every signal: one with a calculated take-profit target and a second "runner" that relies on trailing and break-even logic.

## Details

- **Entry Criteria**:
  - **Long**: The previous bar closed above the previous lower Bollinger band after closing below it two bars ago, the current close also sits above that lower band, and the price is above both higher-timeframe moving averages.
  - **Short**: The previous bar closed below the previous upper Bollinger band after closing above it two bars ago, the current close also sits below that upper band, and the price is below both higher-timeframe moving averages.
- **Position Management**:
  - Two positions are opened per signal using `First Volume` (with take-profit) and `Second Volume` (runner).
  - Stops are anchored to the most recent ZigZag swing extreme minus/plus `Pivot Offset (pts)`.
  - Break-even protection shifts the stop to entry plus an offset once the unrealized profit exceeds `Break-even Threshold (pts)` + `Break-even Offset (pts)`.
  - Trailing stop moves after price advances by `Trailing Step (pts)` beyond the existing stop while maintaining a distance of `Trailing Stop (pts)`.
- **Take Profit**:
  - The first position's take-profit is calculated as a percentage (`Take Profit %`) of the distance between entry and stop.
  - The runner position has no fixed target and exits via stop, trailing, or opposite signals.
- **Additional Logic**:
  - Opposite signals immediately close any open positions in the other direction before entering new trades.
  - Signal processing uses closed candles; partial data is ignored.
- **Default Values**:
  - `First Volume` = 0.1
  - `Second Volume` = 0.1
  - `Take Profit %` = 50
  - `Pivot Offset (pts)` = 10
  - `Use Break-even Move` = true
  - `Break-even Offset (pts)` = 80
  - `Break-even Threshold (pts)` = 10
  - `Trailing Stop (pts)` = 80
  - `Trailing Step (pts)` = 120
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `Base Candle` = 1-hour candles
  - `MA1 Candle` = 1-day candles
  - `MA2 Candle` = 4-hour candles
  - `MA1 Period` = 20
  - `MA2 Period` = 20
  - `ZigZag Depth` = 12
  - `ZigZag Deviation (pts)` = 5
  - `ZigZag Backstep` = 3
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Bollinger Bands, Moving Averages, ZigZag
  - Stops: Yes (swing stop, break-even, trailing)
  - Complexity: Advanced
  - Timeframe: Multi-timeframe (1h base, Daily + 4h filters)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Notes

- The strategy requires candle subscriptions on three distinct timeframes to evaluate the filters and manage exits.
- Swing detection approximates the MetaTrader ZigZag logic by enforcing minimum depth, deviation, and backstep rules before updating pivot levels.
- Volumes can be adjusted independently to tune the size of the take-profit leg versus the runner leg.
