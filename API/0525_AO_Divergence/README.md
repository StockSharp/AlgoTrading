# AO Divergence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy looks for bullish and bearish divergences between the Awesome Oscillator (AO) and price. A bullish divergence occurs when price makes a lower low while AO forms a higher low. A bearish divergence appears when price makes a higher high while AO makes a lower high.

When a bullish divergence is detected, the strategy opens a long position. A bearish divergence triggers a short position. Positions reverse on opposite signals.

## Details

- **Entry Criteria**: AO bullish or bearish divergence with price.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite divergence signal.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = 5 minute
  - `FastLength` = 5
  - `SlowLength` = 34
  - `Lookback` = 5
  - `UseEma` = false
- **Filters**:
  - Category: Indicator
  - Direction: Both
  - Indicators: Awesome Oscillator
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
