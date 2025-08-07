# Williams VIX Fix Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Williams VIX Fix strategy adapts Larry Williams’ volatility indicator to instruments that lack a published VIX. It computes a synthetic VIX value using the distance between the highest close over a lookback period and the current low. When this value rises above a Bollinger Band threshold or the price closes below the lower Bollinger Band, the strategy considers it an oversold opportunity. An inverted calculation gauges overbought extremes.

The approach looks for mean reversion after volatility spikes. When the VIX Fix signals high fear and price is below the lower band, a long trade is opened. Conversely, when the inverse VIX Fix points to extreme complacency and price is above the upper band, existing long positions are closed. Percentile thresholds control sensitivity.

## Details

- **Entry Criteria**:
  - VIX Fix ≥ upper band or percentile and price < lower Bollinger Band.
- **Long/Short**: Long entries with exits on opposite signal.
- **Exit Criteria**:
  - Inverted VIX Fix ≥ upper band or percentile and price > upper Bollinger Band.
- **Stops**: None.
- **Default Values**:
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `WvfPeriod` = 20
  - `WvfLookback` = 50
  - `HighestPercentile` = 0.85
  - `LowestPercentile` = 0.99
- **Filters**:
  - Category: Volatility mean reversion
  - Direction: Long
  - Indicators: Bollinger Bands, Williams VIX Fix
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
