# RPoint 250 Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **RPoint 250 Reversal Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `e_RPoint_250`. The original robot
relies on a custom indicator called *RPoint* that highlights the most recent swing high and swing low. Because that indicator is
not available on StockSharp, the conversion reproduces the same behaviour with the built-in `Highest` and `Lowest` indicators.
Whenever a new extreme replaces the previously detected one, the strategy immediately flips the position and restores the same
stop-loss, take-profit and trailing logic defined in the MQL version.

## Trading workflow

1. Subscribe to the candle series specified by `CandleType` (default: 5-minute candles).
2. Track the rolling maximum and minimum over the last `ReversePoint` bars. These values represent the emulated RPoint levels.
3. If price prints a new highest high, close any long position and open a short position with volume `OrderVolume`.
4. If price prints a new lowest low, close any short position and open a long position with volume `OrderVolume`.
5. Apply protective orders using `StartProtection`. The stop-loss and take-profit distances are expressed in price points via
   the parameters `StopLossPoints` and `TakeProfitPoints`.
6. Optionally trail profits by `TrailingStopPoints`. The trailing engine measures how far price has moved in favour of the
   position and closes it when price retraces by the configured number of points.
7. Remember the candle time of the last successful entry to avoid opening multiple trades within the same bar, matching the
   `TimeN` safeguard from the MQL script.

The strategy always maintains at most one open position. It closes existing trades before entering in the opposite direction and
never scales in.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Volume sent with each market order. Mirrors the `Lots` input in the MetaTrader version. |
| `TakeProfitPoints` | `decimal` | `15` | Distance to the take-profit order measured in price points. Set to `0` to disable profit targets. |
| `StopLossPoints` | `decimal` | `999` | Distance to the protective stop expressed in price points. Set to `0` to trade without a fixed stop. |
| `TrailingStopPoints` | `decimal` | `0` | Optional trailing distance in price points. When zero, the trailing logic is disabled. |
| `ReversePoint` | `int` | `250` | Number of candles considered when searching for the latest swing high and swing low. Larger values smooth out noise. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Candle aggregation analysed by the strategy. Change it to match the chart timeframe used in MetaTrader. |

## Implementation notes

- `Highest` and `Lowest` are bound to the candle subscription via the high-level `Bind` API, so no manual indicator queues are
  required.
- `StartProtection` reproduces the original stop-loss and take-profit distances in absolute price units. StockSharp handles the
  order placement once a new position appears.
- Trailing stops are implemented by monitoring each completed candle. When price retreats by the configured number of points from
  the best price achieved after entry, the position is closed with a market order.
- The class stores the most recent executed reversal levels (`_executedHighLevel` and `_executedLowLevel`) to avoid duplicate
  entries. This is equivalent to the `Reverse_High` / `Reverse_Low` variables in the MQL code.
- The `_lastSignalTime` field mirrors the `TimeN` variable and blocks multiple orders inside the same candle, preventing
  accidental double submissions on illiquid markets.

## Usage guidelines

1. Attach the strategy to a portfolio that supports the selected instrument and candle type.
2. Adjust `OrderVolume` to comply with the contract size and risk management rules of your broker.
3. Tune `ReversePoint` to match the volatility of the traded asset. Higher values yield fewer but more meaningful reversals.
4. Verify that `StopLossPoints`, `TakeProfitPoints` and `TrailingStopPoints` are compatible with the security's `PriceStep`.
5. Run a backtest in StockSharp Designer or Backtester to confirm the behaviour before trading live capital.
6. Monitor the log output: informational messages will highlight position changes and can help validate the conversion.

Because the RPoint indicator is approximated with built-in components, minor differences from the MetaTrader execution are
possible on historical data with gaps or different rounding rules. Always validate the results with your own market data feeds
before relying on the strategy in production.
