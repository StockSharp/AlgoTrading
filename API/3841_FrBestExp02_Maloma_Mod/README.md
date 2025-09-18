# FrBestExp02 Maloma Mod Strategy

This strategy is a C# port of the MetaTrader 4 expert `Frbestexp02_1_maloma_mod.mq4`. It combines OsMA momentum, fractal reversals, tick volume confirmation and a rolling daily pivot filter to fade exhausted moves on the M15 timeframe.

## Trading logic

- **Session pivot** – a rolling pivot point is computed from the highest high, lowest low and the oldest close inside a configurable window (96 candles by default, equal to one trading day on M15). Only trades that agree with the pivot bias are allowed: shorts above the pivot and longs below it.
- **Fractal pattern** – the strategy waits for a confirmed Bill Williams fractal three candles back. Down fractals (swing lows) enable shorts, while up fractals (swing highs) enable longs.
- **OsMA histogram** – a MACD histogram (fast 12, slow 26, signal 9 by default) must be sloping further into negative territory for shorts and higher into positive territory for longs. The previous histogram reading also has to be on the same side of zero.
- **Volume filter** – the volume of the previous finished candle must exceed a configurable threshold and be larger than the volume two candles ago. This reproduces the tick volume spike requirement from the original expert.
- **Order timing** – trades are throttled by a minimum interval (20 seconds by default) between entries.
- **Risk management** – configurable stop-loss, take-profit and optional trailing stop are expressed in points and converted to instrument prices. Protective orders are updated with the built-in `SetStopLoss`/`SetTakeProfit` helpers.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Order volume used for every entry. | 1 |
| `StopLossPoints` | Stop-loss distance in instrument points. | 1000 |
| `TakeProfitPoints` | Take-profit distance in instrument points. | 1000 |
| `TrailingStopPoints` | Optional trailing stop distance in points (0 disables trailing). | 0 |
| `VolumeThreshold` | Minimum previous candle volume required to enable a signal. | 50 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | MACD parameters used to compute the OsMA histogram. | 12 / 26 / 9 |
| `PivotWindow` | Number of finished candles included in the pivot calculation. | 96 |
| `MinTradeIntervalSeconds` | Minimum number of seconds between new entries. | 20 |
| `CandleType` | Primary timeframe (defaults to 15-minute candles). | M15 |

## Differences versus the MQL4 expert

- The original code supported hedging orders multiplied by `kh` and complex profit recycling logic. The StockSharp version executes a single directional position and closes or reverses it before opening a new trade.
- Trailing stop handling is simplified to use the standard `SetStopLoss` helper instead of manually modifying orders per tick.
- Profit aggregation and martingale-style recovery blocks are omitted. Exit management relies on stop-loss, take-profit or trailing stop.
- All indicator calculations are event-driven on finished candles. There is no intrabar order modification.

## Usage notes

1. Attach the strategy to an instrument that supplies tick volume data if the volume filter should match the original behaviour.
2. Keep the timeframe at 15 minutes to reproduce the original calibration of the pivot window and fractal lookback.
3. Adjust the `VolumeThreshold` and OsMA periods to fit symbols with different volatility or volume profiles.
4. Enable the trailing stop only when a tighter exit is desired; otherwise leave it at zero to rely on the static stop/target.

The code follows the high-level StockSharp API guidelines: candle subscriptions via `SubscribeCandles`, indicator binding for the MACD histogram, and safe execution through `BuyMarket`/`SellMarket` with automatic protective orders.
