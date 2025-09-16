# NRTR ATR Stop Strategy

## Overview
The NRTR ATR Stop strategy reproduces the behaviour of the MetaTrader expert `Exp_NRTR_ATR_STOP` using StockSharp's high-level API. It tracks the Non-Repainting Trailing Reverse (NRTR) levels built from the Average True Range (ATR). When price crosses the opposite trailing stop the trend flips, generating a fresh market entry while also closing any open position in the previous direction.

## Indicator logic
* A single **Average True Range** (`AtrPeriod`) is calculated from the subscribed candle series. The ATR value is multiplied by the `Coefficient` to produce the distance between price and the current stop level.
* Two dynamic stop lines are maintained:
  * `upper stop` protects long positions. It trails below price while the trend is bullish.
  * `lower stop` protects short positions. It trails above price while the trend is bearish.
* When price closes beyond the opposite stop the trend reverses immediately. The stop on the new side is initialised using the previous candle's extremum minus/plus the ATR distance.
* The original expert delays execution by reading indicator buffer `SignalBar` candles back. The strategy mirrors this behaviour through an internal queue: every finished candle pushes its signal into the queue and the engine acts only when the queue length exceeds `SignalBar`.

## Trading rules
1. **Buy signal** – the calculated trend changes from neutral/bearish to bullish. The strategy optionally closes any short exposure and opens a fresh long position using a single market order whose volume equals the required exit size plus the configured `Volume` for the new long entry.
2. **Sell signal** – the trend changes from neutral/bullish to bearish. The strategy optionally closes any long exposure and opens a new short position in the same way.
3. The properties `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, and `EnableShortExit` allow precise control over which actions are executed when a signal appears.
4. Signals are processed only on finished candles and while the strategy is online and allowed to trade.

## Parameters
| Name | Description |
| --- | --- |
| `AtrPeriod` | Number of candles used for ATR calculation. |
| `Coefficient` | Multiplier applied to the ATR value when constructing the trailing stops. |
| `SignalBar` | Number of fully closed candles to wait before acting on a stored signal. Set to `0` to trade immediately on the current candle. |
| `CandleType` | Time frame of the incoming candles. |
| `EnableLongEntry` | Allow opening long positions on buy signals. |
| `EnableShortEntry` | Allow opening short positions on sell signals. |
| `EnableLongExit` | Allow closing long positions when a sell signal occurs. |
| `EnableShortExit` | Allow closing short positions when a buy signal occurs. |

## Notes
* The strategy relies solely on finished candles; intrabar ticks are ignored.
* Orders are submitted with `BuyMarket`/`SellMarket`, combining position closure and fresh entry into a single market order for simplicity.
* Ensure the `Volume` property is set to a positive value before starting live trading or backtesting.
