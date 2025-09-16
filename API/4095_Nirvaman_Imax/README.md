# Nirvaman Imax Strategy

## Overview
The Nirvaman Imax strategy is a direct conversion of the MetaTrader 4 expert advisor `NirvamanImax.mq4` bundled with the HA, Moving Averages2 and iMAX3alert custom indicators. The StockSharp implementation keeps the original idea of combining Heikin-Ashi candles with a two-phase trend detector and an EMA baseline filter while adopting the high-level API. The strategy works on a single instrument and timeframe and automatically closes trades after a configurable holding period.

## Indicators and filters
- **Heikin-Ashi candles** – reproduce the original HA indicator and classify candles as bullish or bearish by comparing the Heikin open and close values.
- **Fast/slow EMA crossover** – replaces the MT4 `iMAX3alert1` double-phase output. A bullish signal appears when the fast EMA crosses above the slow EMA; a bearish signal occurs on the opposite crossover.
- **EMA trend filter** – mirrors the `Moving Averages2` EMA buffer and acts as a baseline. Only long trades above the filter and short trades below it are allowed.
- **Time filter** – skips any candle whose hour lies inside the forbidden window defined by `NoTradeStartHour` and `NoTradeEndHour` (the window supports wrap-around midnight and a broker time-zone shift).
- **Timed exit** – every position is force-closed after `CloseAfter` elapses, reproducing the `tiempoCierre` logic of the MQL version.
- **Stops and targets** – stop loss and take profit are applied in price steps derived from the instrument tick size. Setting either to `0` disables the corresponding protection.

## Trading rules
1. Wait until the Heikin-Ashi, fast EMA, slow EMA and filter EMA are formed and a previous candle close is available.
2. Reject the signal if the candle time is inside the restricted trading window.
3. Long entry:
   - Fast EMA crosses above the slow EMA on the current candle.
   - The Heikin-Ashi close is above its open (bullish body).
   - The previous candle close is above the EMA filter.
4. Short entry:
   - Fast EMA crosses below the slow EMA on the current candle.
   - The Heikin-Ashi close is below its open (bearish body).
   - The previous candle close is below the EMA filter.
5. Exit rules:
   - Stop loss or take profit levels are touched by the candle range.
   - The maximum position lifetime `CloseAfter` is exceeded.
   - Manual protection triggered via `StartProtection()` closes the position when the engine requests it.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Base market order volume. | `0.1` |
| `CandleType` | Candle timeframe used for every indicator and signal. | `30m` time frame |
| `FastTrendLength` | Length of the fast EMA that emulates the blue iMAX phase. | `10` |
| `SlowTrendLength` | Length of the slow EMA that emulates the red iMAX phase. | `21` |
| `FilterLength` | EMA period for the baseline filter (Moving Averages2 equivalent). | `13` |
| `StopLoss` | Protective stop distance in price steps; `0` disables the stop. | `50` |
| `TakeProfit` | Profit target distance in price steps; `0` disables the target. | `100` |
| `CloseAfter` | Maximum holding time before the position is force-closed. | `15000 s` |
| `NoTradeStartHour` | Hour (0–23) that marks the beginning of the no-trade window. | `22` |
| `NoTradeEndHour` | Hour (0–23) that marks the end of the no-trade window. | `2` |
| `BrokerTimeOffset` | Broker time zone offset (hours) applied before the time filter. | `0` |

## Conversion notes
- The MT4 `iMAX3alert1` indicator exposes two colour-coded buffers. Their crossover is translated into a fast/slow EMA crossover, which preserves the original event-driven entry logic.
- The Moving Averages2 indicator ran in EMA mode with a default length of 13. The StockSharp version reuses a standard `ExponentialMovingAverage` with the same default.
- Position life-cycle management mirrors the MQL script: the position is closed on time-out before new entries can be evaluated, and no additional trailing stop logic was added.

## Usage tips
1. Attach the strategy to a board/security and set the desired `CandleType` before starting it.
2. Adjust `TradeVolume`, `StopLoss`, `TakeProfit` and `CloseAfter` to match the instrument volatility and risk tolerance.
3. Optimise the EMA periods if you need to approximate the behaviour of the original iMAX tuning for a new market.
4. Combine with higher level risk controls (portfolio protection, session control) when running multiple instances.
