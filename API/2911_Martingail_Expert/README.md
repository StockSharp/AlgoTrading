# Martingail Expert Strategy

## Overview
- Port of the MetaTrader 5 **MartingailExpert.mq5** expert adviser.
- Uses a stochastic oscillator crossover with configurable %K, %D and slowing parameters to open positions.
- Implements a martingale-style grid with both averaging and breakout entries that scale volume geometrically.
- Designed for netted portfolios – the strategy maintains a single aggregated long or short position.

## Trading Logic
### Entry Conditions
1. The strategy processes closed candles of the `CandleType` timeframe.
2. Stochastic values are taken from the previous finished candle to mimic the MQL `iStochastic(..., 1)` call.
3. A long entry is triggered when:
   - Previous %K is greater than previous %D.
   - Previous %D is above `BuyLevel`.
   - No open position exists.
4. A short entry is triggered when:
   - Previous %K is below previous %D.
   - Previous %D is below `SellLevel`.
   - No open position exists.
5. All market orders use the normalized `Volume` value (rounded to the nearest `Security.VolumeStep`).

### Position Scaling
- `ProfitPips` defines the distance (in pips) required to add another base position in the direction of profit.
  - When long, if the candle high reaches `lastEntryPrice + ProfitPips * positionCount`, a new order with the base `Volume` is sent.
  - When short, if the candle low reaches `lastEntryPrice - ProfitPips * positionCount`, a new base order is sent.
- `StepPips` defines the averaging distance (in pips) to apply the martingale multiplier.
  - For longs, if the candle low touches `lastEntryPrice - StepPips`, the next order volume equals `lastVolume * Multiplier`.
  - For shorts, if the candle high touches `lastEntryPrice + StepPips`, the same martingale sizing is applied.
- Every executed trade updates `lastEntryPrice`, `lastVolume`, and the internal count of active positions.

### Exit Logic
- The last executed trade price is stored per direction.
- If price reaches `lastEntryPrice ± ProfitPips` (using candle highs for longs and lows for shorts), all open positions are closed via market order.
- Once the aggregated position returns to zero, martingale state variables are reset.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `0.03` | Base lot size for the initial order and profit-based add-ons. |
| `Multiplier` | `1.6` | Martingale multiplier for averaging entries. |
| `StepPips` | `25` | Pip distance that triggers averaging orders against the trend. |
| `ProfitPips` | `9` | Pip distance used for both profit exits and breakout add-ons. |
| `KPeriod` | `5` | Lookback period of the stochastic %K calculation. |
| `DPeriod` | `3` | Smoothing period for the stochastic %D line. |
| `Slowing` | `3` | Smoothing applied to the %K line (slow stochastic). |
| `BuyLevel` | `20` | Minimum %D value required to allow long entries. |
| `SellLevel` | `55` | Maximum %D value required to allow short entries. |
| `CandleType` | `5 minute` time frame | Timeframe used to build candles and indicators. |

## Implementation Notes
- Pip distance is computed from `Security.PriceStep`. Instruments with 3 or 5 decimal quotes are automatically adjusted by multiplying the price step by 10 to match the original MQL pip logic.
- Volumes are rounded down to the nearest `Security.VolumeStep`. Values that fall below the minimum tradable step are ignored.
- The strategy relies on candle highs and lows to approximate intra-bar triggers because the high-level API operates on finished candles.
- `OnOwnTradeReceived` tracks real execution prices and volumes to faithfully reproduce the martingale escalation sequence.

## Usage Tips
- Align the `CandleType` with the timeframe used in the original MetaTrader template (commonly M5) for similar behaviour.
- Ensure the security metadata (price step, volume step) is populated; otherwise adjust `Volume`, `StepPips`, and `ProfitPips` manually to match broker specifications.
- Consider enabling external risk management (stop losses or capital limits) because the martingale logic intentionally increases exposure during adverse moves.

## Differences from the Original Expert Advisor
- The StockSharp version processes completed candles instead of every tick; threshold checks use candle highs/lows to approximate intra-bar behaviour.
- MetaTrader-specific account margin checks are unavailable in StockSharp high-level strategies; ensure adequate capital is configured externally.
- Order execution and position tracking leverage StockSharp’s netting model; hedging mode is not supported.
