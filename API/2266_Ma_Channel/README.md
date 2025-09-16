# Ma Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Ma Channel strategy trades breakouts of a moving average channel built from the
high and low prices. A position is opened when price leaves the channel in the
corresponding direction and reversed when the trend flips. The channel boundaries
are calculated from exponential moving averages with a fixed offset.

The system is designed for both long and short trading and reacts only on finished
candles. It aims to catch trend transitions early while avoiding noise inside the
channel.

## Details

- **Entry Conditions**:
  - **Long**: Price breaks above the upper channel.
  - **Short**: Price breaks below the lower channel.
- **Exit Conditions**:
  - Opposite breakout triggers a reversal of the position.
- **Indicators**: Exponential moving averages of highs and lows with configurable
  length and price offset.
- **Stops**: Not used by default, trades are closed only on opposite signals.
- **Default Values**:
  - `Length` = 8
  - `Offset` = 10
  - `CandleType` = 1 hour candles
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Simple
  - Timeframe: Medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
