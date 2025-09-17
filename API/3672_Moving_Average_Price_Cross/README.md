# Moving Average Price Cross Strategies

## Overview

This package contains two C# strategy ports of the MetaTrader 5 examples located in `MQL/50198`:

* **`MovingAveragePriceCrossStrategy`** – a minimalistic moving average versus price crossover system that trades a single position at a time.
* **`MovingAverageMartingaleStrategy`** – an enhanced version that applies martingale-style position sizing after losses while preserving the same price/average crossover logic.

Both implementations rely on the high-level StockSharp API, use candle subscriptions for signal evaluation, and expose MetaTrader-compatible parameters for stop-loss and take-profit distances.

## Files

| File | Description |
| --- | --- |
| `CS/MovingAveragePriceCrossStrategy.cs` | Base price/MA crossover using fixed volume and static protective orders. |
| `CS/MovingAverageMartingaleStrategy.cs` | Martingale variant that scales volume and protective distances after losing trades. |

## Trading logic

### MovingAveragePriceCrossStrategy

1. Subscribes to candles of the configured timeframe and calculates a simple moving average (`SMA`).
2. Evaluates signals only on finished candles to mimic the MT5 expert behaviour.
3. Detects crossovers between the SMA and the candle close price using the last two completed candles:
   * **Sell** when the moving average rises above the candle close (price crossed below the average).
   * **Buy** when the moving average drops below the candle close (price crossed above the average).
4. Places a single market order per signal if no position is currently open.
5. Applies automatic protection via `StartProtection` with MetaTrader point distances converted into absolute price offsets.

### MovingAverageMartingaleStrategy

1. Shares the same candle subscription and SMA signal generation as the base strategy.
2. Tracks realized PnL after each closed position and stores the last trade result.
3. When a new crossover signal appears and no position is open:
   * If the last trade was **loss-making**, multiplies the next trade volume by `VolumeMultiplier` (capped at `MaxVolume`) and enlarges the stop-loss and take-profit distances by `TargetMultiplier`.
   * If the last trade was **profitable**, resets the trade volume and protective distances to their initial values.
4. Applies `StartProtection` with the dynamically adjusted offsets immediately before sending the market order.
5. Continues to trade only one position at a time, matching the original expert advisor logic.

## Risk management

* Protective levels are expressed in MetaTrader points and automatically translated into absolute price offsets using the detected pip size (`PriceStep` adjusted for 3/5 decimal FX symbols).
* The martingale strategy keeps the stop-loss and take-profit multipliers bounded to prevent runaway distances.
* Position volume is aligned with the instrument's `VolumeStep`, `MinVolume`, and optional `MaxVolume` to avoid invalid orders.

## Parameters

### Shared inputs

| Parameter | Strategy | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | both | `1 minute` | Candle data type used for signal calculation. |
| `MaPeriod` | both | `50` | Length of the simple moving average. |

### MovingAveragePriceCrossStrategy

| Parameter | Default | Description |
| --- | --- | --- |
| `OrderVolume` | `1` | Order volume aligned to the instrument step. |
| `TakeProfitPoints` | `150` | Take-profit distance in MetaTrader points (0 disables). |
| `StopLossPoints` | `150` | Stop-loss distance in MetaTrader points (0 disables). |

### MovingAverageMartingaleStrategy

| Parameter | Default | Description |
| --- | --- | --- |
| `StartingVolume` | `1` | Base volume restored after profitable trades. |
| `MaxVolume` | `5` | Maximum volume after applying multipliers. |
| `TakeProfitPoints` | `100` | Initial take-profit distance in MetaTrader points. |
| `StopLossPoints` | `300` | Initial stop-loss distance in MetaTrader points. |
| `VolumeMultiplier` | `2` | Factor applied to the next order volume after a loss. |
| `TargetMultiplier` | `2` | Factor applied to stop-loss and take-profit distances after a loss. |

## Usage notes

* MetaTrader “points” correspond to one `PriceStep` for most instruments; the strategies automatically multiply by 10 for 3- or 5-decimal FX symbols to match MT5 behaviour.
* Both strategies require only one security and will ignore signals while a position is open, reproducing the original experts’ `PositionsTotal()` guard.
* Enable optimization on the exposed parameters inside the StockSharp designer to replicate MT5 input tuning.
