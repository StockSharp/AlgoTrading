# Balance Drawdown In MT4 Strategy

This strategy ports the original MetaTrader 4 expert advisor **BalanceDrawdownInMT4** to the StockSharp high-level API. The EA immediately opens a single long position and continuously measures the account drawdown relative to the peak balance reached since the session started.

## Trading Logic

1. When the strategy starts it calls `StartProtection` to arm managed stop-loss and take-profit levels that mimic the MQL inputs expressed in price points.
2. On the first finished candle (default timeframe: 1 minute) the strategy verifies whether a position is open. If no exposure exists it submits a market buy order using the configured `Volume`.
3. After every finished candle it updates the drawdown metric:
   - The strategy tracks the maximum achieved balance as **StartBalance + realized PnL**.
   - The current equity equals **StartBalance + realized PnL + unrealized PnL**, where unrealized PnL is derived from the latest candle close price, the average entry price, and the instrument's `PriceStep`/`StepPrice`.
   - Drawdown is the percentage decline from the stored peak balance to the current equity. The value is logged with an informational message on every update.

The algorithm never opens additional positions or reverses. Once the initial position is established it remains active until stopped out, the take-profit fires, or the user manually intervenes.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `StartBalance` | `1000` | Baseline balance used when calculating peak equity and drawdown percentage. |
| `Volume` | `0.01` | Net volume (in instrument units) of the initial market buy order. |
| `StopLossPoints` | `300` | Distance from the entry price to the protective stop, measured in price points. A value of `0` disables the stop. |
| `TakeProfitPoints` | `400` | Distance from the entry price to the protective target, measured in price points. A value of `0` disables the target. |
| `CandleType` | `1m` time frame | Time frame that drives periodic drawdown updates and the initial entry check. |

## Implementation Notes

- The drawdown counter uses the strategy's realized PnL (`PnL`) combined with the unrealized PnL estimated from price differences, matching the running balance logic found in the MT4 version.
- If `PriceStep` or `StepPrice` is unavailable for the security, the unrealized PnL calculation safely returns zero, preventing divide-by-zero errors.
- `Volume` is validated to ensure a positive value before the initial trade; otherwise a warning is logged and the strategy stays flat.
- `DrawdownPercent` exposes the latest drawdown reading so that other modules (dashboards, risk controllers) can pull the value programmatically.

## Usage Tips

- Set `StartBalance` to the real account balance (or the balance at the beginning of the trading session) to obtain meaningful drawdown statistics.
- Keep the default 1-minute candles for timely updates, or choose a faster synthetic candle type if you need near-tick precision.
- Because this strategy intentionally holds a single long position, pair it with manual risk controls or external automation if you need to re-enter after a stop or target is hit.
- Always test on a simulator to confirm that the broker supplies `PriceStep` and `StepPrice` so the unrealized PnL conversion matches expectations.
