# Trendless AG Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades reversals detected by the **Trendless AG Histogram** indicator. The indicator measures the distance between price and a smoothed moving average and then smooths the result again, forming a histogram around zero. Local minima indicate potential upward reversals while local maxima suggest downward reversals.

Positions are opened when the histogram changes direction. If the indicator rises after being below previous values, a long position is opened. If it falls after being above previous values, a short position is opened. Optional stop-loss and take-profit levels manage risk.

## Details

- **Entry Criteria**:
  - **Long**: Histogram value is rising while the previous value was lower than its predecessor.
  - **Short**: Histogram value is falling while the previous value was higher than its predecessor.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Opposite signal or stop-loss/take-profit levels.
- **Stops**: Fixed stop-loss and take-profit in price units.
- **Default Values**:
  - `Fast Length` = 7.
  - `Slow Length` = 5.
  - `Stop Loss` = 1000.
  - `Take Profit` = 2000.
  - `Candle Type` = 12-hour candles.
- **Filters**:
  - Category: Trend following.
  - Direction: Both.
  - Indicators: Custom indicator built from moving averages.
  - Stops: Yes.
  - Complexity: Moderate.
  - Timeframe: Medium-term.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: Yes.
  - Risk level: Medium.
