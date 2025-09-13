# Parabolic SAR Alert Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy monitors the Parabolic SAR (Stop and Reverse) indicator to detect potential trend reversals. When the SAR value flips from above the price to below, the algorithm interprets it as a bullish signal and opens a long position. When the SAR moves from below the price to above, a short position is opened.

The default acceleration factor (0.02) and maximum acceleration (0.2) follow the classic Parabolic SAR configuration. These parameters control how quickly the indicator approaches price: higher values make the SAR react faster but can lead to whipsaws. The strategy processes only finished candles and stores previous SAR and price values to identify crossovers without querying historical data.

Risk management is not defined explicitly; the example relies on opposite signals to exit. Additional protection can be enabled through the framework's built-in mechanisms.

## Details

- **Entry Criteria**: Parabolic SAR crosses the closing price.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: Not defined.
- **Default Values**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
