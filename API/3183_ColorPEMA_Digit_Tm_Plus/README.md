# Exp Color PEMA Digit Tm Plus Strategy

## Overview
The **Exp Color PEMA Digit Tm Plus Strategy** is a direct port of the MetaTrader 5 expert advisor "Exp_ColorPEMA_Digit_Tm_Plus". The strategy rebuilds the original Pentuple Exponential Moving Average (PEMA) indicator and reproduces every trading permission flag present in the EA. Orders are executed on the selected candle series only after the indicator confirms a color flip and the optional waiting period (`Signal Bar`) has elapsed.

The StockSharp version keeps the same money-management options, stop/target controls, and time-based exit that existed in the MQL implementation. Each setting is exposed through `StrategyParam<T>` to support UI configuration and optimization.

## Indicator logic
* The indicator feeds a cascade of eight exponential moving averages using the configured `PEMA Length` and `Applied Price`.
* The final line is rounded to the requested `Rounding Digits`, exactly as in the original indicator.
* The slope of the rounded line produces three states:
  * **Up (magenta)** – bullish pressure, potential long setup.
  * **Flat (gray)** – neutral, no action.
  * **Down (dodger blue)** – bearish pressure, potential short setup.
* The strategy stores the indicator state of every completed candle so that it can reference older bars when `Signal Bar` is greater than zero.

## Trading rules
1. **Signal detection** – on a finished candle, evaluate the indicator state that is `Signal Bar` candles old and compare it with the previous state.
2. **Long setup** – when the state turns to *Up* from anything else:
   * queue a long entry if `Allow Long Entries` is enabled;
   * queue an exit of existing shorts if `Allow Short Exits` is enabled.
3. **Short setup** – when the state turns to *Down* from anything else:
   * queue a short entry if `Allow Short Entries` is enabled;
   * queue an exit of existing longs if `Allow Long Exits` is enabled.
4. **Execution layer** – queued actions are executed only when
   * the strategy is online and trading is permitted;
   * the activation timestamp tied to the source candle has been reached; and
   * position sizing rules allow a non-zero volume.
5. **Risk management** –
   * optional stop-loss and take-profit levels are derived from the fill price using the same point distances as in MetaTrader;
   * `Use Time Exit` closes positions that exceed the configured `Holding Minutes` lifetime;
   * opposite signals can immediately flatten exposure if the respective exit permission is active.

## Parameters
| Name | Description |
| ---- | ----------- |
| Money Management | Base value used by the position sizing rules. |
| Money Mode | Chooses between lot-based sizing or balance/free-margin percentage models. |
| Stop Loss (points) | Distance to the stop loss in price points. |
| Take Profit (points) | Distance to the take profit in price points. |
| Allowed Deviation | Placeholder parameter preserved from the EA for completeness. |
| Allow Long Entries / Allow Short Entries | Enable or disable opening trades in each direction. |
| Allow Long Exits / Allow Short Exits | Enable or disable closing trades when opposite signals appear. |
| Use Time Exit | Activates the time-based flatting logic. |
| Holding Minutes | Maximum holding time of a position, expressed in minutes. |
| Candle Type | Candle series processed by the strategy. Defaults to H4. |
| PEMA Length | Length used for all eight EMA stages in the Pentuple EMA. |
| Applied Price | Source price used in the indicator calculation. |
| Rounding Digits | Decimal digits used to round the indicator output. |
| Signal Bar | Number of completed bars to wait before evaluating a signal. |

## Usage notes
* Place the strategy inside a StockSharp connector that provides access to the desired instrument and candle series.
* Configure the parameters to match the MetaTrader setup you want to replicate.
* Run backtests or live trading as needed; the strategy reacts only to fully closed candles.

## Conversion status
* **C# version** – implemented (`CS/ExpColorPemaDigitTmPlusStrategy.cs`).
* **Python version** – not created (per instruction).
