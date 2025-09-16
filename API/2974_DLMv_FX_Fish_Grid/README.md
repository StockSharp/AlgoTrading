# DLMv FX Fish Grid Strategy

## Overview

The **DLMv FX Fish Grid Strategy** replicates the behaviour of the original MetaTrader expert advisor built around the "FX Fish 2MA" oscillator. The strategy evaluates the Fisher Transform of price, smooths it with a moving average and opens positions when the oscillator crosses its smoothed baseline on the appropriate side of zero. Position management mimics the grid-like behaviour of the source EA: additional entries are spaced by a configurable distance, pending limit orders can be layered, and protective automation handles risk controls.

## Trading Logic

1. **Indicator calculation**
   - Highest and lowest prices over `CalculatePeriod` candles define the rolling range.
   - A Fisher Transform is applied to the selected price (`AppliedPrice`), using the same 0.67 smoothing factor as the MT5 indicator.
   - A simple moving average (`MaPeriod`) of the Fisher value provides the signal baseline.
2. **Signal generation**
   - **Long signal**: current and previous Fisher values are below zero while the oscillator crosses **above** its moving average (previous value below average, current value above).
   - **Short signal**: current and previous Fisher values are above zero while the oscillator crosses **below** the moving average (previous value above average, current value below).
   - Signals can be inverted by enabling `ReverseSignals`.
3. **Order execution**
   - When a buy (or sell) signal appears, the strategy can optionally close existing opposite exposure (`CloseOpposite`).
   - Additional entries are allowed until the total count reaches `MaxTrades`. Every new entry must respect the minimum spacing given by `DistancePips` from the latest filled trade.
   - Optional limit orders (`SetLimitOrders`) place resting bids/asks at the configured spacing, replicating the staged grid from the original EA.
4. **Risk management**
   - Fixed stop-loss, take-profit and trailing stop values are applied via `StartProtection`, all defined in pips.
   - `TimeLiveSeconds` closes all exposure when a trade has been open longer than the allowed lifetime.
   - Trading can be disabled during Fridays (`TradeOnFriday = false`). When disabled the strategy closes positions and cancels pending orders as soon as a Friday candle arrives.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Order size for each entry (lots). |
| `StopLossPips` | Distance of the protective stop-loss from the entry. Set to 0 to disable. |
| `TakeProfitPips` | Distance of the take-profit level. Set to 0 to disable. |
| `TrailingStopPips` | Trailing stop distance (0 disables trailing). |
| `TrailingStepPips` | Step by which the trailing stop is tightened. |
| `MaxTrades` | Maximum number of simultaneous trades per direction. `0` removes the limit. |
| `DistancePips` | Minimum distance between consecutive entries and for the optional grid orders. |
| `TradeOnFriday` | When `false`, the strategy stops trading on Fridays and liquidates exposure. |
| `TimeLiveSeconds` | Maximum time (seconds) that positions may remain open before being force-closed. |
| `ReverseSignals` | Invert long/short conditions. |
| `SetLimitOrders` | Enable additional resting limit orders at `DistancePips`. |
| `CloseOpposite` | Close opposite exposure before entering a new trade. |
| `CalculatePeriod` | Lookback for the Fisher Transform range. |
| `MaPeriod` | Period of the moving average applied to the Fisher value. |
| `AppliedPrice` | Price source used in the Fisher Transform (close, open, high, low, median, typical, weighted). |
| `CandleType` | Data type / timeframe of the candles processed by the strategy. |

## Notes

- The stop-loss, take-profit and trailing stop distances are converted from pips to absolute price offsets using `Security.PriceStep * 10`, matching the five-digit pip logic of the MQL version.
- Limit orders are automatically cancelled when signals flip, trading is paused, or lifetime/Friday protections trigger.
- The Fisher Transform avoids repeated value lookups, instead storing the previous oscillator and baseline readings for precise cross detection.
