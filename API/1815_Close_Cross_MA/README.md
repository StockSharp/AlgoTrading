# Close Cross MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy monitors a simple moving average (MA) and automatically closes any open position when the candle close crosses the MA line. It is designed for traders who manage entries manually or with other systems but want an automated exit once the trend reverses.

The logic tracks the relationship between the close price and the MA. When a new finished candle crosses from one side of the MA to the other, the strategy sends a market order to flatten the position. No new positions are opened.

## Details

- **Entry Criteria**: None. Positions must be opened externally.
- **Exit Criteria**:
  - **Long**: Previous close above MA and current close below MA triggers a sell to close.
  - **Short**: Previous close below MA and current close above MA triggers a buy to close.
- **Long/Short**: Both directions are supported.
- **Stops**: Not used. The MA cross acts as the exit signal.
- **Default Values**:
  - `MA Period` = 50.
  - `Candle Type` = Time frame of 1 minute.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate

