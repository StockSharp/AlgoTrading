# Neuro Nirvaman MQ4 Strategy

## Overview
The **Neuro Nirvaman MQ4 Strategy** is a faithful port of the MetaTrader 4 expert advisor `NeuroNirvaman.mq4`. The original robot combines a custom Laguerre filter applied to the +DI component of the ADX indicator with a SilverTrend breakout detector. Three perceptrons evaluate these inputs and a supervisor decides whether to buy or sell. The StockSharp version mirrors that workflow and executes one position at a time, recalculating its logic only on fully closed candles.

## How the strategy works
1. **Market data feed** – The strategy subscribes to a single candle series defined by `CandleType` and processes only `Finished` candles. It does not evaluate intrabar events, replicating the `Time[0]` check used in MT4.
2. **Laguerre +DI smoothing** – Four `AverageDirectionalIndex` indicators provide +DI values which are sent through a Laguerre filter (`LaguerrePlusDiState`) using the original gamma of 0.764. The filter yields oscillator values in the `[0, 1]` range and each stream has its own ADX period and neutral zone width (`Laguerre*Distance`).
3. **SilverTrend port** – Two `SilverTrendState` objects reproduce the `Sv2.mq4` logic. They track the highest high and lowest low for `SSP` candles, shrink the channel with the constant `Kmax = 50.6`, and return `1` for an uptrend or `-1` for a downtrend. The lookback depths are controlled by `SilverTrend1Length` and `SilverTrend2Length`.
4. **Perceptrons** –
   - *Perceptron #1* mixes the first Laguerre activation with the first SilverTrend swing using weights `X11 - 100` and `X12 - 100`.
   - *Perceptron #2* combines the second Laguerre activation with the second SilverTrend swing and weights `X21 - 100` and `X22 - 100`.
   - *Perceptron #3* evaluates the third and fourth Laguerre activations weighted by `X31 - 100` and `X32 - 100`.
   Each Laguerre activation is quantised to `-1`, `0` or `1` depending on its distance from the 0.5 equilibrium level.
5. **Supervisor (`Pass`)** – The supervisor reproduces the MQL `Supervisor()` function:
   - `Pass = 3`: requires `Perceptron #3 > 0`. If also `Perceptron #2 > 0`, the strategy buys using the second TP/SL set; otherwise if `Perceptron #1 < 0`, it sells using the first TP/SL set.
   - `Pass = 2`: a positive `Perceptron #2` opens a long with the second TP/SL set, while any non-positive value opens a short with the first set.
   - `Pass = 1`: a negative `Perceptron #1` opens a short, otherwise a long is opened. Both branches use the first TP/SL set.
6. **Order and risk management** – Entries are sent with `BuyMarket` or `SellMarket` using `TradeVolume`. Take-profit and stop-loss levels are calculated as `entry ± points * PriceStep`. Because StockSharp places pure market orders, protective exits are simulated by checking candle highs and lows, exactly like broker-side TP/SL orders would trigger in MT4.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15-minute time frame | Candle type processed by the strategy. |
| `TradeVolume` | `decimal` | 0.1 | Order volume in lots. |
| `SilverTrend1Length` | `int` | 7 | Lookback length for the first SilverTrend calculation (SSP). |
| `Laguerre1Period` | `int` | 14 | ADX period for the first Laguerre stream. |
| `Laguerre1Distance` | `decimal` | 0 | Neutral zone width (percent) around 0.5 for Laguerre stream #1. |
| `X11`, `X12` | `decimal` | 100 | Weights used inside perceptron #1 (the code subtracts 100 before applying them). |
| `TakeProfit1`, `StopLoss1` | `decimal` | 100 / 50 | Protective distances in points for the first risk profile and all short trades. |
| `SilverTrend2Length` | `int` | 7 | Lookback length for the second SilverTrend calculation. |
| `Laguerre2Period` | `int` | 14 | ADX period for the second Laguerre stream. |
| `Laguerre2Distance` | `decimal` | 0 | Neutral zone width (percent) around 0.5 for Laguerre stream #2. |
| `X21`, `X22` | `decimal` | 100 | Weights used inside perceptron #2. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 100 / 50 | Protective distances in points for the second risk profile. |
| `Laguerre3Period`, `Laguerre4Period` | `int` | 14 | ADX periods for the third and fourth Laguerre streams. |
| `Laguerre3Distance`, `Laguerre4Distance` | `decimal` | 0 | Neutral zone widths (percent) for the third and fourth Laguerre streams. |
| `X31`, `X32` | `decimal` | 100 | Weights used inside perceptron #3. |
| `Pass` | `int` | 3 | Supervisor branch that selects which perceptrons can trigger trades. |

## Usage notes
- Default weights of `100` neutralise the corresponding perceptron input. Move weights away from 100 to create meaningful signals.
- SilverTrend starts returning `±1` once enough candles are collected. Until then, perceptron outputs may stay at zero, emulating the MT4 behaviour where `iCustom` returns zero before buffers are ready.
- Take-profit and stop-loss checks rely on candle extremes; if intra-candle spikes occur between bars, the simulation may diverge slightly from broker-side execution.
- Only one position can exist at a time. A new signal is ignored until the current position is closed either by TP, SL or an opposite decision.
- Adjust `CandleType` to mirror the chart period used by the original MT4 setup (for example M15 or H1) to keep indicator scaling consistent.
