# Gann Line Strategy

This strategy replicates the core ideas of the MetaTrader 4 "Gann Line" expert advisor (source ID 24877) using the StockSharp high-level API. It keeps the same trend, momentum and long-term MACD filters while expressing all money-management tools in **price steps**, which makes the logic broker-independent.

## Trading Logic

1. **Trend filter (primary timeframe)**
   - Two linear weighted moving averages (LWMA) are applied to the typical candle price (high + low + close) / 3.
   - A long bias requires the fast LWMA to close above the slow LWMA; a short bias requires the opposite.
2. **Momentum confirmation (higher timeframe)**
   - A momentum oscillator calculated on a configurable higher timeframe checks how far the oscillator deviates from the equilibrium level (100).
   - At least one of the last three finished momentum values must exceed the configured deviation threshold before any trade is allowed.
3. **Slow MACD filter (very high timeframe)**
   - A MACD filter calculated on a slow timeframe (monthly by default) must confirm the direction: MACD main line above signal for longs, below for shorts.
4. **Position management**
   - Fixed stop-loss and take-profit targets are converted from price steps into absolute prices when a trade opens.
   - Optional break-even logic moves the stop to the entry price plus an offset once the trade has moved a given amount of steps in profit.
   - Optional trailing logic shifts the stop behind the highest high (for longs) or lowest low (for shorts) once the price has travelled a configurable number of steps.

## Risk Management

- All distances (stop-loss, take-profit, break-even and trailing) are entered in price **steps**. The helper converts them to prices by using the instrument `PriceStep`.
- The strategy works with the base `Volume` property. If it is zero, one contract/lot is used by default.
- Only a single net position is managed. Opposite signals close the current trade before opening a new one.

## Differences from the MQL4 Version

- The original expert advisor relied on a manually drawn Gann trend line. StockSharp does not expose chart objects, so the port replaces that check with the LWMA slope confirmation.
- Money-based trailing, partial closes and account-wide equity checks from the script are simplified into deterministic step-based calculations.
- Notifications (alerts, e-mails, mobile pushes) are not generated because StockSharp strategies typically log to the platform output.

## Parameters

| Name | Description |
| --- | --- |
| `Fast LWMA` | Length of the fast LWMA used for the trend filter. |
| `Slow LWMA` | Length of the slow LWMA used for the trend filter. |
| `Momentum Period` | Lookback of the momentum oscillator on the secondary timeframe. |
| `Momentum Threshold` | Minimum deviation from 100 required by any of the last three momentum values. |
| `MACD Fast / Slow / Signal` | EMA lengths of the slow MACD filter. |
| `Take Profit (steps)` | Take-profit distance in price steps. |
| `Stop Loss (steps)` | Stop-loss distance in price steps. |
| `Use Trailing`, `Trail Activation`, `Trail Distance` | Enable trailing, profit needed before trailing starts, and distance between price extreme and trailing stop. |
| `Use BreakEven`, `BreakEven Activation`, `BreakEven Offset` | Enable break-even, profit required before moving the stop, and additional profit locked afterwards. |
| `Primary Timeframe` | Candle type used by the LWMA crossover. |
| `Momentum Timeframe` | Candle type forwarded into the momentum oscillator. |
| `MACD Timeframe` | Candle type forwarded into the slow MACD filter. |

## Usage Tips

1. Select an instrument and set the desired `Primary Timeframe`. The other timeframes default to 1 hour (momentum) and 30 days (MACD) but can be customised to reproduce the original coefficient mapping.
2. Configure `Volume` and the step-based risk parameters to match your broker contract specifications.
3. Run the strategy in `Designer` or through code. Monitor the log to verify that filters, break-even moves and trailing adjustments appear as expected.
4. Optimise momentum and MACD thresholds to adapt the ported logic to different markets or timeframes.

## Further Enhancements

- Integrate an equity-based global stop similar to the original script.
- Replace the LWMA slope filter with a custom chart-drawn trend line once StockSharp exposes object events.
- Add partial profit-taking to mimic the multi-target behaviour of the MQL4 version.
