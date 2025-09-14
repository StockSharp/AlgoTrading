# LeMan Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The LeMan Trend strategy derives bullish and bearish pressure from recent highs and lows. It calculates the distance between the current candle and the highest highs and lowest lows over three different lookback periods. These distances are smoothed with an exponential moving average (EMA) to form two lines: bulls and bears. A crossover between these lines signals potential trend changes.

When the bulls line crosses above the bears line, the strategy opens a long position or closes an existing short position. Conversely, when the bears line moves above the bulls line, it opens a short position or exits a long one. The method does not use additional filters, focusing solely on the relative strength of recent highs and lows.

## Details

- **Entry Criteria**
  - **Long**: Bulls line crosses above bears line.
  - **Short**: Bears line crosses above bulls line.
- **Long/Short**: Both sides supported.
- **Exit Criteria**
  - Opposite crossover closes the active position.
- **Stops**: None by default.
- **Default Values**
  - `Min` = 13
  - `Midle` = 21
  - `Max` = 34
  - `EMA period` = 3
  - `Time frame` = 4 hours
- **Filters**
  - Category: Trend following
  - Direction: Both
  - Indicators: Highest, Lowest, EMA
  - Stops: No
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
