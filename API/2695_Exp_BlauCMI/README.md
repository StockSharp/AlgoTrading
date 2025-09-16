# Exp BlauCMI

## Overview
The strategy recreates the MetaTrader 5 expert advisor **Exp_BlauCMI** using StockSharp's high level API. It computes the Blau Candle Momentum Index (CMI), a triple-smoothed momentum ratio, on a configurable candle series and reacts to swings in the oscillator. Long trades are opened when the indicator turns upward after a downswing, short trades are opened when the indicator turns downward after an upswing. The module keeps the implementation fully event driven – orders are sent only after candles are closed.

## Indicator logic
1. Two price sources are selected through `Momentum Price` and `Reference Price`. The raw momentum is the difference between the current value of the first price and the delayed value of the second price. The delay is controlled by `Momentum Depth`.
2. Both the momentum and its absolute value are passed through three consecutive moving averages (`First/Second/Third Smoothing`). The same averaging method is used for every stage and can be selected among simple, exponential, smoothed (RMA) and linear weighted moving averages.
3. The Blau CMI is calculated as `100 * smoothedMomentum / smoothedAbsMomentum`. The indicator starts producing trading signals once the third smoothing stage has accumulated enough bars.
4. The `Signal Shift` parameter determines how many closed candles back the strategy inspects before evaluating reversals (a value of 1 reproduces the original EA and uses the last closed bar).

## Trading rules
- **Long entry** – allowed when `Allow Long Entry` is enabled and the indicator sequence `Value[Signal Shift - 1] < Value[Signal Shift - 2]` followed by `Value[Signal Shift] > Value[Signal Shift - 1]` is observed, meaning the oscillator just turned upward. Existing short positions are closed first if `Allow Short Exit` is enabled.
- **Short entry** – allowed when `Allow Short Entry` is enabled and the indicator turns downward (`Value[Signal Shift - 1] > Value[Signal Shift - 2]` and `Value[Signal Shift] < Value[Signal Shift - 1]`). Existing long positions are closed beforehand if `Allow Long Exit` is enabled.
- **Long exit** – when in a long position and the short-entry condition triggers, the position is closed if `Allow Long Exit` is true.
- **Short exit** – when in a short position and the long-entry condition triggers, the position is closed if `Allow Short Exit` is true.
- All trades are executed with market orders using the volume specified in `Order Volume`. Protective stop-loss and take-profit brackets are attached automatically via `StartProtection` and remain active while the position is open.

## Parameters
- `Candle Type` – data type (timeframe or other candle description) used for indicator computation and trading decisions. Default is 4-hour candles.
- `Smoothing Method` – averaging algorithm shared by all three smoothing stages (Simple, Exponential, Smoothed, Linear Weighted).
- `Momentum Depth` – number of bars between the two price points that form raw momentum.
- `First/Second/Third Smoothing` – lengths of the three averaging stages applied to both the momentum and its absolute value.
- `Signal Shift` – number of already closed candles to look back when evaluating reversal patterns (minimum value is 1).
- `Momentum Price` – applied price used for the non-delayed leg of the momentum calculation.
- `Reference Price` – applied price used for the delayed comparison leg.
- `Allow Long Entry`, `Allow Short Entry` – toggles to permit opening trades in each direction.
- `Allow Long Exit`, `Allow Short Exit` – toggles controlling whether opposite signals close the respective positions.
- `Stop-Loss Points`, `Take-Profit Points` – risk limits measured in price steps (`Security.PriceStep`). When set to zero the corresponding bracket is disabled.
- `Order Volume` – absolute quantity used when sending market orders. The strategy also assigns this value to the base `Strategy.Volume` property.

## Additional notes
- The supported smoothing methods correspond to StockSharp indicators: Simple Moving Average, Exponential Moving Average, Smoothed Moving Average (RMA) and Weighted Moving Average.
- The Demark price constant replicates the MT5 implementation by averaging price extremes and the candle close before adjusting the high/low distances.
- Because calculations use only finished candles, the strategy reacts once per bar, matching the original EA behaviour that checked for new bars via `IsNewBar`.
- `Stop-Loss Points` and `Take-Profit Points` are interpreted as multiples of the instrument price step to stay consistent with the point-based inputs of the original MQL5 strategy.
