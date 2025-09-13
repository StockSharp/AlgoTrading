# Profit Trailing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Converted from the MQL4 expert advisor "ProfitTrailing.mq4". This utility strategy monitors an existing position and manages its exit using a fixed take profit and a trailing stop in profit. It can be attached to any other trading strategy or used manually to supervise open trades.

The strategy starts trailing once unrealized profit exceeds a configurable step. The trailing level then moves along with growing profit. If the profit retraces by the step amount or reaches the specified take profit, the position is closed.

## Details

- **Entry Criteria**:
  - None. Positions must be opened by other strategies or manually.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Profit hits `TakeProfit`.
  - After activation, profit falls back to the trailing stop.
- **Stops**: Trailing stop in profit only; no initial stop loss.
- **Filters**:
  - Works on any instrument and timeframe.
- **Parameters**:
  - `TrailingStep` — profit (in currency units) required to activate trailing and distance of the trailing stop.
  - `TakeProfit` — profit (in currency units) to close the position.
  - `CandleType` — candle series used to track price and profit.

## Mechanics

1. On start a candle subscription is opened and `StartProtection()` is called.
2. When a position appears the strategy remembers the entry price.
3. After profit exceeds `TrailingStep` a trailing stop is set at `profit - TrailingStep`.
4. The trailing stop moves up as profit grows.
5. The position is closed if profit drops below the trailing stop or reaches `TakeProfit`.
