# ADX Stop Order Template Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy demonstrates how to place pending stop orders using the Average Directional Index (ADX) and its Directional Movement components. It recreates the core logic of a classic MQL template: when the market shows a strong trend and the +DI and -DI lines cross, the system places a buy stop or sell stop at a fixed distance. Protective stop-loss and take-profit levels are managed automatically.

The example is intentionally simple and focuses on order handling. Traders can extend it with additional filters or money management rules to build more advanced systems.

## Details

- **Entry Criteria**:
  - ADX value above the `ADX Threshold` parameter.
  - **Long**: `+DI` greater than `-DI` and two candles ago `+DI` was below `-DI`.
  - **Short**: `+DI` less than `-DI` and two candles ago `+DI` was above `-DI`.
  - Current spread must be below the `Max Spread` parameter.
- **Order Placement**:
  - Pending stop orders are placed `Pips` price steps away from the current bid or ask.
  - Only one pending order is active at a time; older orders are cancelled when a new signal appears.
- **Exit Criteria**:
  - Long positions are closed when `-DI` rises above `+DI`.
  - Short positions are closed when `+DI` rises above `-DI`.
- **Stops**:
  - Stop-loss and take-profit are applied via `StartProtection` using `Stop Loss` and `Take Profit` parameters.
- **Default Values**:
  - `ADX Period` = 14
  - `ADX Threshold` = 5
  - `Pips` = 10 price steps
  - `Take Profit` = 1000 price steps
  - `Stop Loss` = 500 price steps
  - `Max Spread` = 20 price steps
  - `Candle Type` = 15-minute candles
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ADX, DMI
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday
  - Spread filter: Yes
