# Altarius RSI Stochastic Dual Strategy

## Overview

Altarius RSI Stochastic Dual Strategy is a conversion of the MetaTrader expert advisor `AltariusRSIxampnSTOH`. The logic combines two stochastic oscillators with a short-period RSI filter. The slow stochastic identifies trend direction and overbought/oversold zones, while the fast stochastic measures momentum strength. Exits rely on RSI and the slow stochastic signal line to trail winning trades and cut losses. Additional money management features mirror the original MQL logic by reducing position size after losses and enforcing an equity drawdown limit.

## Trading Logic

1. **Data Source** – The strategy works on configurable candles (default 15-minute bars). All calculations use candle close data.
2. **Entry Conditions**
   - **Long setup**: Slow stochastic main line (15,8,8) is above its signal line yet still below the `BuyStochasticLimit` (50 by default). Fast stochastic (10,3,3) shows momentum with an absolute difference between main and signal lines above the `StochasticDifferenceThreshold` (5 by default).
   - **Short setup**: Slow stochastic main line is below its signal line but remains above the `SellStochasticLimit` (55 by default). The fast stochastic must again show a difference greater than the momentum threshold.
3. **Exit Conditions**
   - **Long exit**: Triggered when the RSI (period 4) exceeds `ExitRsiHigh` (60) and the slow stochastic signal line declines below its previous value while staying above `ExitStochasticHigh` (70).
   - **Short exit**: Triggered when the RSI drops under `ExitRsiLow` (40) and the slow stochastic signal line rises above its previous value while staying below `ExitStochasticLow` (30).
   - **Risk exit**: If floating PnL drops below the allowed equity drawdown (`MaximumRiskPercent`), all positions are flattened immediately.
4. **Position Sizing** – Starts with `BaseVolume` and reduces the effective size after consecutive losing trades via `DecreaseFactor`. Broker volume constraints are respected using the security volume step and limits.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `BaseVolume` | Base order size before risk management adjustments. |
| `MaximumRiskPercent` | Percentage of account equity that can be lost before the strategy forcefully closes positions. |
| `DecreaseFactor` | Divider controlling how quickly position size contracts after consecutive losses. |
| `RsiPeriod` | RSI length used for exit decisions. |
| `SlowStochasticPeriod`, `SlowStochasticK`, `SlowStochasticD` | Configuration for the slow stochastic oscillator that drives trend direction. |
| `FastStochasticPeriod`, `FastStochasticK`, `FastStochasticD` | Configuration for the fast stochastic oscillator that measures momentum. |
| `StochasticDifferenceThreshold` | Minimum distance between fast stochastic main and signal lines to confirm momentum. |
| `BuyStochasticLimit`, `SellStochasticLimit` | Slow stochastic levels that define the acceptable trading zone for new positions. |
| `ExitRsiHigh`, `ExitRsiLow` | RSI levels that prepare long or short exits. |
| `ExitStochasticHigh`, `ExitStochasticLow` | Slow stochastic signal levels that finalize exits. |
| `CandleType` | Candle data source for indicator calculations. |

## Notes

- The strategy trades a single position at a time, mirroring the original expert advisor behaviour.
- Volume adjustments and drawdown protection are calculated using the current portfolio information available in StockSharp.
- Chart visualization draws candles, both stochastic oscillators, and trade markers when a chart area is available.
