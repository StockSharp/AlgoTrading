# PowerZone Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy identifies "PowerZone" order blocks created by a bearish candle followed by consecutive bullish candles (and vice versa). A breakout above the bull zone triggers a long trade, while a breakdown below the bear zone opens a short. Targets and stops are based on the zone's range.

## Details

- **Entry Criteria**:
  - **Long**: Bearish candle `Periods+1` bars ago followed by `Periods` bullish candles and price breaking above the zone high.
  - **Short**: Bullish candle `Periods+1` bars ago followed by `Periods` bearish candles and price breaking below the zone low.
- **Long/Short**: Both sides.
- **Exit Criteria**: Take profit and stop loss multiples of the zone range.
- **Indicators**: None.
- **Default Values**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
