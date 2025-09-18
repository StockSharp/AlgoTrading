# Dealers Trade v7.51 RIVOT (C#)

## Summary

Dealers Trade v7.51 is a martingale-style grid strategy that was originally delivered as the MetaTrader 4 expert advisor `Dealers_Trade_v_7.51_RIVOT.mq4`. The port keeps the original idea of trading away from a pivot-based directional bias, scaling into the dominant side whenever price retraces by a configurable pip distance. The StockSharp implementation uses high-level strategy helpers to subscribe to candles, calculate the pivot zones, and manage position sizing, risk, and exits.

## Trading Logic

1. **Pivot framework**
   - The strategy builds two reference prices on every finished candle:
     - **Classic pivot** (`P`) = `(previous high + previous low + previous close + current open) / 4`.
     - **Floating pivot** (`FLP`) = `(current high + current low + current close) / 3`.
   - A gap in pips between `P` and `FLP` must be greater than or equal to `GapThreshold` to enable trading for the current bar.

2. **Directional bias**
   - When the candle close is above both pivots and the gap filter is satisfied, the bias switches to **long**.
   - When the candle close is below both pivots with the gap confirmed, the bias switches to **short**.
   - The bias remains in force until the position series is fully closed or the opposite condition appears after the series ends.

3. **Scaling entries**
   - Only one series of trades can be active at a time.
   - The first entry follows the bias immediately.
   - Additional entries are opened only when price retraces against the active bias by at least `PipDistance` pips from the most recent fill, emulating the original martingale averaging.
   - Each new order multiplies the previous size by `VolumeMultiplier` but never exceeds `MaxVolume`.
   - The number of stacked entries is limited by `MaxTrades`.

4. **Risk controls**
   - A hard stop-loss at `StopLoss` pips from the volume-weighted average entry closes the entire series.
   - A fixed take-profit at `TakeProfit` pips locks in gains once price reverts in favor.
   - When enabled, the trailing-stop dynamically locks profits by stepping closer to price every time it moves further than `TrailingStop` pips beyond the average entry.

5. **Reset conditions**
   - Any full exit (stop-loss, take-profit, trailing-stop or manual position flattening) resets the martingale counters and removes the directional bias.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | 1 | Base order size for the first entry. |
| `MaxTrades` | 5 | Maximum number of averaged entries per series. |
| `PipDistance` | 4 | Minimum adverse movement (in pips) required before adding a new position. |
| `TakeProfit` | 15 | Distance from the volume-weighted average entry to close the entire grid in profit. |
| `StopLoss` | 90 | Distance from the average entry that triggers a protective exit. |
| `TrailingStop` | 15 | Trailing-stop offset applied once price travels in favor; set to zero to disable trailing. |
| `VolumeMultiplier` | 1.5 | Factor used to increase the order size for each subsequent entry. |
| `MaxVolume` | 5 | Cap for the single-order volume after applying the multiplier. |
| `GapThreshold` | 7 | Minimum gap (in pips) between the classic and floating pivots required to activate the bias. |
| `CandleType` | 15-minute time-frame candles | Candle type used for calculations and decision making. |

All parameters are configured through `StrategyParam<T>` so they can be optimized inside StockSharp Designer or Strategy Runner.

## Usage Notes

- The strategy relies on candle data only; no direct tick-level bid/ask stream is required. Ensure that your data provider can deliver the selected `CandleType`.
- Because StockSharp aggregates positions by default, the implementation maintains an internal volume-weighted average to emulate the MT4 grid book. If partial fills occur, the built-in position accounting keeps the values consistent.
- Chart rendering adds two horizontal lines (`Pivot` and `FloatingPivot`) to the chart area when it is available.
- There is no automatic reverse trading; the system waits for the ongoing series to finish before accepting a bias flip.

## Differences from the MQL Version

- The original script drew multiple labels and comments on the MT4 chart. The port keeps only functional trading logic and replaces the visuals with StockSharp chart lines.
- Account-protection features based on total open orders, manual magic number filtering, and symbol-specific pip value tables are not required in StockSharp and were omitted.
- Order closure at exact tick prices (`Ask == tp`) in the MetaTrader code is approximated with price comparisons on candle closes.
- Trade management is implemented with market orders (`BuyMarket`/`SellMarket`) instead of MT4 ticket loops. Trailing stops and exits happen on candle updates.

## Best Practices

- Always test the strategy in paper trading or historical simulations with realistic spread/commission models before going live.
- Consider lowering `VolumeMultiplier` or `MaxTrades` on highly volatile instruments to control drawdown.
- For intraday products, adjust `CandleType` to match the data granularity of the original setup (the default is 15 minutes, but the EA was frequently used on M15 and H1).

## Files

- `CS/DealersTradeV751RivotStrategy.cs` – Main C# implementation.
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.

