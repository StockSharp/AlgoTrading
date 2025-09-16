# Two MA Four Level Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader expert "2MA_4Level" using the StockSharp high-level API. It trades a single instrument with two smoothed moving averages (SMMA) calculated on the median price and watches five relative crossover zones between the fast and slow curves. Entries are only allowed when no position is open, and every trade is protected by pip-based stop-loss and take-profit offsets.

## Logic

- Compute a fast and a slow SMMA on the selected candle series (default 50 and 130 periods).
- Evaluate the previous and current SMMA values on the completed candle to detect a crossover.
- Check the crossover against five thresholds built from the slow MA:
  - the raw slow MA (no offset),
  - slow MA + `MostTopLevel` pips,
  - slow MA + `TopLevel` pips,
  - slow MA - `LowermostLevel` pips,
  - slow MA - `LowerLevel` pips.
- When the fast MA crosses above any threshold, open a long position (if flat). A cross below any threshold opens a short position.
- Stop-loss and take-profit levels are attached through `StartProtection` using the instrument pip value (`Security.PriceStep`).

The strategy never pyramids positions: a new trade can only be opened after the previous one is closed by stop or target.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `FastPeriod` | 50 | Length of the fast smoothed moving average. Must be lower than `SlowPeriod`. |
| `SlowPeriod` | 130 | Length of the slow smoothed moving average. |
| `MostTopLevel` | 500 | Upper offset (in pips) used for the widest bullish/bearish confirmation. Must be greater than `TopLevel`. |
| `TopLevel` | 250 | Upper offset (in pips) for the secondary bullish/bearish confirmation. |
| `LowerLevel` | 250 | Lower offset (in pips) for the secondary bearish/bullish confirmation. Must be lower than `LowermostLevel`. |
| `LowermostLevel` | 500 | Lower offset (in pips) used for the widest bearish/bullish confirmation. |
| `TakeProfitPips` | 55 | Distance from entry to the take-profit, expressed in pips. |
| `StopLossPips` | 260 | Distance from entry to the stop-loss, expressed in pips. |
| `CandleType` | 15-minute time frame | Candle series used for the SMMA calculations and signal processing. |

## Implementation Details

- Median price (`(High + Low) / 2`) feeds both SMMAs, matching the MT5 configuration that uses `PRICE_MEDIAN`.
- The crossover test compares the latest completed candle with the previous one, eliminating any reliance on partially formed bars.
- `StartProtection` wires the stop-loss and take-profit once at start-up, so every order inherits the configured risk limits automatically.
- The strategy stops itself during `OnStarted` if invalid parameter combinations are provided (e.g., `FastPeriod >= SlowPeriod`).

## Usage Notes

1. Attach the strategy to an instrument with a defined `PriceStep`; otherwise, the pip conversion falls back to a value of `1`.
2. Suitable for hedging accounts in MT5; in StockSharp it behaves the same by ensuring only one open position at a time.
3. Optimisation hooks (`SetCanOptimize`) are enabled for both MA periods, allowing you to run parameter sweeps directly from the StockSharp optimizer.
4. Because the strategy relies exclusively on stop-loss and take-profit exits, ensure the configured distances align with the instrument volatility to avoid prolonged exposure.

## Files

- `CS/TwoMaFourLevelStrategy.cs` – C# implementation of the trading logic.
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.
