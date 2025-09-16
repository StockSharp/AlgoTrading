# CMO Duplex Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy is a StockSharp port of the MetaTrader 5 expert `Exp_CMO_Duplex.mq5`. It splits the logic into two independent legs
(long and short) that both react to zero-line crossovers of the Chande Momentum Oscillator (CMO). Each leg can consume its own
candle series, period and signal offset, which makes it possible to run asymmetric configurations on the same instrument.

## How it works

- The strategy subscribes to one or two candle feeds depending on whether the long and short legs use the same `DataType`.
- Every leg owns its own CMO indicator instance. The indicator is evaluated on finished candles only.
- The `SignalBar` setting defines how many completed candles back in history should be used for the crossover logic. A value of 0
  means "use the most recent closed bar", `1` uses the previous bar, `2` uses the bar before that, and so on.
- **Long leg:** when the selected CMO value crosses from above zero to zero or below, the strategy enters (or flips into) a long
  position if long entries are allowed. Long exits are triggered when the older value of the CMO is below zero or when the stop
  loss / take profit levels are touched.
- **Short leg:** mirrors the long logic. A crossover from below zero to zero or above opens (or flips into) a short position and
  the opposite sign of the CMO value or the configured stops flat the position.
- Position flips reuse `Volume` plus any opposite exposure, therefore a single market order both closes the previous position and
  opens the new one.
- `StartProtection()` is enabled on launch, so the built-in StockSharp risk controls remain active.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `LongCandleType` | Candle type used by the long leg. |
| `LongCmoPeriod` | Period of the CMO indicator on the long side. |
| `LongSignalBar` | Number of closed bars between the current time and the bar analysed for signals (0 = latest closed bar). |
| `EnableLongEntries` | Allows or blocks opening new long positions. |
| `EnableLongExits` | Allows or blocks closing long positions on oscillator signals. |
| `LongStopLossPoints` | Stop-loss distance in price steps for long trades (0 disables the stop). |
| `LongTakeProfitPoints` | Take-profit distance in price steps for long trades (0 disables the target). |
| `ShortCandleType` | Candle type used by the short leg. |
| `ShortCmoPeriod` | Period of the CMO indicator on the short side. |
| `ShortSignalBar` | Number of closed bars between the current time and the bar analysed for short signals. |
| `EnableShortEntries` | Allows or blocks opening new short positions. |
| `EnableShortExits` | Allows or blocks closing short positions on oscillator signals. |
| `ShortStopLossPoints` | Stop-loss distance in price steps for short trades (0 disables the stop). |
| `ShortTakeProfitPoints` | Take-profit distance in price steps for short trades (0 disables the target). |

The base `Strategy.Volume` property controls the default order size. When the strategy needs to flip direction it sends a market
order whose volume equals `Volume + |Position|`, which closes the old exposure and opens the new one in a single transaction.

## Risk management

- Stop-loss and take-profit levels are evaluated on every finished candle. For long positions the stop is placed below the entry
  and the target above it; for short positions the levels are mirrored.
- A stop or target triggers an immediate market order to flat the position. The same exit routine also runs when the respective
  oscillator value keeps the wrong sign (below zero for longs, above zero for shorts).
- Setting the distance to zero disables the corresponding protection and leaves the leg managed purely by the oscillator logic.

## Usage notes

- The strategy works best on instruments where the CMO tends to revert after touching the zero line. Contrarian entries are
  deliberately delayed by the `SignalBar` offset to match the original expert.
- Long and short legs can share the same candle feed or operate on different timeframes. If both use the same `DataType`, the
  strategy reuses a single subscription for better performance.
- Because the strategy operates on completed candles, it is recommended to supply a continuous candle stream (for example via a
  historical backtest or real-time feed) to avoid missing signals.
