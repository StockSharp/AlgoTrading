# RSI Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Relative Strength Index (RSI) histogram to detect reversals when the oscillator leaves extreme zones. The histogram colors the RSI value based on two thresholds: a high level marking the overbought area and a low level marking the oversold area. When the color changes from green (overbought) to gray or red, the strategy closes short positions and opens a long position. When the color changes from red (oversold) to gray or green, it closes long positions and opens a short position.

The implementation is built with the high-level StockSharp API and subscribes to candle data of a selected timeframe. An RSI indicator processes the candles and generates signals whenever its value exits the defined zones. Optional parameters allow enabling or disabling entries and exits for each side separately.

The strategy is meant for educational purposes and demonstrates how to convert an MQL expert advisor to the StockSharp framework.

## Details

- **Entry Criteria**:
  - **Long**: Previous bar was above the high level and the last bar moved below it.
  - **Short**: Previous bar was below the low level and the last bar moved above it.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal closes the current position if allowed.
- **Stops**: No built-in stops; the `StartProtection` framework is prepared for adding them.
- **Default Values**:
  - `RSI period` = 14
  - `High level` = 60
  - `Low level` = 40
  - `Timeframe` = 4 hours
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Single
  - Stops: Optional
  - Complexity: Simple
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
