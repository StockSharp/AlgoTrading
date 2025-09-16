# Auto SL-TP Setter Strategy

Utility strategy that automatically attaches stop-loss and take-profit orders to open positions when they are missing. Distances can be defined as fixed pip values or multiples of the Average True Range (ATR).

## Parameters

- `Candle Type` – timeframe used for ATR calculation.
- `Set Stop Loss` – enable automatic stop-loss placement.
- `Set Take Profit` – enable automatic take-profit placement.
- `Stop Loss Method` – 1 = fixed pips, 2 = ATR multiple.
- `Fixed SL (pips)` – stop-loss distance in pips for the fixed method.
- `SL ATR Multiplier` – ATR multiplier for stop-loss when using the ATR method.
- `Take Profit Method` – 1 = fixed pips, 2 = ATR multiple.
- `Fixed TP (pips)` – take-profit distance in pips for the fixed method.
- `TP ATR Multiplier` – ATR multiplier for take-profit when using the ATR method.
- `ATR Period` – number of periods used for ATR calculation.

## How it works

1. On start the strategy evaluates the configuration.
2. If ATR-based values are requested it subscribes to the specified candle series and calculates ATR.
3. After the ATR value becomes available the strategy calls `StartProtection` with the calculated distances.
4. `StartProtection` places protective orders for any existing position and for future trades opened by the strategy.

The strategy does not generate trading signals; it only manages risk by ensuring that every position has appropriate stop-loss and take-profit levels.
