# Neuro Nirvaman Strategy

## Overview
The Neuro Nirvaman strategy is a direct conversion of the MetaTrader 5 expert advisor *NeuroNirvamanEA*. It recreates the perceptron-based decision tree from the original MQL implementation by combining four Laguerre-smoothed positive directional indicators (+DI) with two SilverTrend swing detectors. The strategy works on finished candles and submits market orders with dynamic take-profit and stop-loss levels defined in points. No trailing stop, averaging or pyramiding is applied – only a single position can exist at any moment.

## Market Inputs and Indicators
- **AverageDirectionalIndex (4 instances)** – each instance is configured with its own period. The strategy reads the +DI component and passes it through a Laguerre filter to obtain smooth oscillator values in the `[0, 1]` range.
- **LaguerrePlusDiState** – an internal helper that reproduces the `laguerre_plusdi.mq5` custom indicator logic, including the four-stage Laguerre smoothing and `CU / (CU + CD)` normalization.
- **SilverTrendState (2 instances)** – a faithful port of the `silvertrend_signal.mq5` logic. It evaluates the last 10 candles (`SSP = 9`) to detect breakout points, and outputs `1` on bearish arrows, `-1` on bullish arrows, or `0` when no arrow is present.
- **Candle Stream** – the strategy subscribes to a single timeframe selected via `CandleType` and processes only finished candles.

## Trading Logic
1. **Signal Preparation**
   - Each Laguerre value is translated into a discrete activation via the `ComputeTensionSignal` helper: values above `0.5 + distance/100` generate `-1`, below `0.5 - distance/100` generate `1`, and the neutral zone produces `0`.
   - SilverTrend swings are updated on every candle. The risk parameters (`Risk1`, `Risk2`) shrink or widen the support/resistance channel exactly as in the MQL indicator.
2. **Perceptrons**
   - **Perceptron 1** mixes the first Laguerre activation with the first SilverTrend swing using weights `X11 - 100` and `X12 - 100`.
   - **Perceptron 2** mixes the second Laguerre activation with the second SilverTrend swing using weights `X21 - 100` and `X22 - 100`.
   - **Perceptron 3** works on the third and fourth Laguerre activations with weights `X31 - 100` and `X32 - 100`.
3. **Supervisor (Pass parameter)**
   - `Pass = 3`: requires `Perceptron 3 > 0`. If also `Perceptron 2 > 0`, the strategy buys using `TakeProfit2` / `StopLoss2`. Otherwise, if `Perceptron 1 < 0`, it sells using `TakeProfit1` / `StopLoss1`.
   - `Pass = 2`: when `Perceptron 2 > 0`, a long position is opened with the second set of risk limits. If `Perceptron 2 <= 0`, a short is opened with the first set of limits.
   - `Pass = 1`: when `Perceptron 1 < 0`, the strategy sells using the first risk set. Otherwise, it goes long using the same risk settings.
4. **Order Management**
   - Entries are executed with `BuyMarket` or `SellMarket` and use the `TradeVolume` parameter as lot size.
   - Take-profit and stop-loss levels are calculated from the close price of the signal candle: `entry ± points * PriceStep`. They are monitored on every finished candle via high/low checks, emulating the original MT5 protective orders.
   - New signals are ignored while a position is active; only when the position is closed are fresh trades evaluated.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15-minute time frame | Candle type used for calculations. |
| `TradeVolume` | `decimal` | 0.1 | Position volume in lots. |
| `Risk1`, `Risk2` | `int` | 3 / 9 | SilverTrend risk factors defining the width of the channel. |
| `Laguerre1Period` – `Laguerre4Period` | `int` | 14 | ADX length for each Laguerre smoothing stream. |
| `Laguerre1Distance` – `Laguerre4Distance` | `decimal` | 0 | Distance in percent (0–100) around the 0.5 threshold that defines the neutral zone. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | `decimal` | 100 | Weight coefficients; the MQL code subtracts 100 before applying them. |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | `int` | 100 / 50 | Protective distances expressed in points. |
| `Pass` | `int` | 3 | Supervisor mode that selects the combination of perceptrons used for trading. |

## Usage Notes
- Default weights (`100`) neutralize the perceptrons. To activate the strategy, adjust the weights away from `100` so that the perceptrons can produce non-zero outputs.
- The SilverTrend implementation respects the original alert count logic (without alerts) and keeps state between candles, so signals align with the MT5 version.
- Because take-profit and stop-loss levels are simulated internally, the high/low of each completed candle is used to check for target hits. Intrabar spikes between ticks are not modeled.
- The strategy is single-symbol and does not manage multiple instruments. Attach it to the desired security and configure the candle series accordingly.
- Only long or short positions are allowed at a time; reversing the position forces a full exit first.

## Deployment
1. Build the solution and run the strategy from the StockSharp samples launcher or include it in a custom project.
2. Choose the security, assign the candle series, and configure the perceptron weights plus risk parameters.
3. Start the strategy and monitor the trades using the automatically created chart (Laguerre indicators and own deals are added to the area).
4. Optimizations can be run through the built-in parameter metadata (`SetCanOptimize`) if desired.
