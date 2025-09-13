# PZ Reversal Trend Following
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy follows breakouts of long-term highs and lows. It buys when the closing price exceeds the highest high of the lookback period and sells short when the closing price falls below the lowest low. The position is always reversed on opposite signals, keeping the strategy continually in the market.

The approach attempts to capture sustained trends by entering after a significant breakout. Because the system trades only at major extremes, it may avoid minor noise but can incur large drawdowns during choppy conditions.

## Details

- **Entry Criteria**: Breakout of previous `Period` bars high/low.
- **Long/Short**: Both directions, always in the market.
- **Exit Criteria**: Opposite breakout signal.
- **Stops**: No.
- **Default Values**:
  - `Period` = 100
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

