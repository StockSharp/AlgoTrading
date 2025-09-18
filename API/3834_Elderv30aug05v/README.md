# Elderv30aug05v Strategy

## Overview
The Elderv30aug05v strategy is a direct port of the MetaTrader 4 expert advisor with the same name. It combines signals from two MACD filters calculated on hourly candles and two stochastic oscillators calculated on 15-minute candles. Trade execution and exit management happen on one-minute candles to replicate the tick-by-tick logic of the original MQL script. The strategy opens at most one position at a time and relies on dynamic trailing stops rather than fixed take-profit orders.

## Indicators and Data
- **Primary MACD** (`13/30/9`, hourly candles). A long signal requires the histogram to slope upward while the previous value remains below zero.
- **Secondary MACD** (`14/56/9`, hourly candles). A short signal requires the histogram to slope downward while the previous value remains above zero.
- **Fast stochastic oscillator** (`%K=2`, `%D=3`, smoothing=3, 15-minute candles). Long entries demand the %K line to be below the configured ceiling (default 36) and rising relative to the previous bar.
- **Slow stochastic oscillator** (`%K=1`, `%D=3`, smoothing=3, 15-minute candles). Short entries require the %K line to be above the configured floor (default 66) and declining relative to the previous bar.
- **One-minute candles** supply the confirmation data for breakout checks and manage trailing stops.

All indicators process only finished candles through `SubscribeCandles().Bind()/BindEx()` to follow the high-level StockSharp API guidelines.

## Entry Rules
### Long Setup
1. The primary MACD value is above its previous reading and the previous reading is negative.
2. The fast stochastic %K is below `LongStochasticThreshold` (default 36) and above its previous value.
3. The close of the current one-minute candle is greater than the high of the previous one-minute candle.

### Short Setup
1. The secondary MACD value is below its previous reading and the previous reading is positive.
2. The slow stochastic %K is above `ShortStochasticThreshold` (default 66) and below its previous value.
3. The close of the current one-minute candle is lower than the low of the previous one-minute candle.

Only one position can be open. If a new signal appears while a position is active, it is ignored until the position is closed by stop-loss or trailing logic.

## Exit Rules
- **Initial stop-loss**: Upon entry the strategy stores the entry price plus/minus `LongStopLoss` or `ShortStopLoss` multiplied by the instrument `PriceStep`. If `PriceStep` is not provided, a fallback of `0.0001` is used.
- **Trailing stop**: Once the price moves in favour of the trade by at least `LongTrailingStop` or `ShortTrailingStop` points (again multiplied by `PriceStep`), the stored stop price is shifted behind the market. For long trades the stop trails the close minus the trailing distance and only moves upwards. For short trades the stop trails the close plus the distance and only moves downwards.
- When the candle range touches the stored stop price, the position is closed at market.

No fixed take-profit level is used, reflecting the original MetaTrader behaviour.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `0.1` | Trade volume sent to `BuyMarket`/`SellMarket`. |
| `LongStopLoss` | `17` | Long stop-loss distance in points. |
| `ShortStopLoss` | `46` | Short stop-loss distance in points. |
| `LongTrailingStop` | `18` | Trailing distance for long positions. |
| `ShortTrailingStop` | `22` | Trailing distance for short positions. |
| `LongStochasticThreshold` | `36` | Maximum fast stochastic %K value for long entries. |
| `ShortStochasticThreshold` | `66` | Minimum slow stochastic %K value for short entries. |
| `BaseCandleType` | `TimeFrame(1m)` | Candle series used for execution logic. |
| `StochasticCandleType` | `TimeFrame(15m)` | Candle series for both stochastic oscillators. |
| `MacdCandleType` | `TimeFrame(1h)` | Candle series for both MACD filters. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | `13 / 30 / 9` | Periods for the primary MACD. |
| `AltMacdFastPeriod` / `AltMacdSlowPeriod` / `AltMacdSignalPeriod` | `14 / 56 / 9` | Periods for the secondary MACD. |
| `StochasticFastKPeriod` / `StochasticFastDPeriod` / `StochasticFastSmooth` | `2 / 3 / 3` | Parameters for the fast stochastic. |
| `StochasticSlowKPeriod` / `StochasticSlowDPeriod` / `StochasticSlowSmooth` | `1 / 3 / 3` | Parameters for the slow stochastic. |

## Notes
- The strategy works with any instrument that provides minute-level candles and a valid `PriceStep`.
- Trailing stops are maintained internally; no protective orders are registered on the exchange side.
- The logic processes only finished candles to avoid repainting and matches the MQL implementation that relied on completed bars.

## Original Script
- **Source**: `MQL/7674/Elderv30aug05v.mq4`
- **Platform**: MetaTrader 4 expert advisor.
