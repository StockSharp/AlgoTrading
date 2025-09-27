# Mean Reversion Momentum Strategy

## Overview
The Mean Reversion strategy is a direct port of the MetaTrader expert advisor *Mean reversion.mq4*. The StockSharp version keeps the original trading idea: buy after an extended series of declining closes and sell after a similar bullish run. Entries are confirmed by trend alignment using two linear weighted moving averages, momentum strength on a higher timeframe, and a monthly MACD filter.

Once in position, the strategy recreates the money management rules from the MQL version: configurable stop-loss and take-profit in pips, optional break-even relocation, and a trailing stop that locks in profits as the market moves in the trade's favor.

## Trading Logic
1. **Signal timeframe** – the strategy operates on the selected candle series (default 15 minutes).
2. **Exhaustion detection** – it collects the last `BarsToCount` closes. A long setup requires the most recent close to be lower than each of the previous closes, signalling a sell-off. A short setup needs the opposite condition.
3. **Trend filter** – fast LWMA (length `FastMaLength`) must be above the slow LWMA (`SlowMaLength`) for longs and below for shorts.
4. **Momentum filter** – the momentum indicator (period `MomentumLength`) is calculated on the MetaTrader-style higher timeframe (M15 → H1, H1 → D1, etc.). At least one of the last three momentum readings must deviate from 100 by more than `MomentumThreshold`.
5. **MACD confirmation** – a monthly MACD (12/26/9) must have the main line above the signal line for longs and below for shorts.

If every condition is satisfied the strategy opens a position using `OrderVolume`. Opposite trades flatten the current position before reversing.

## Position Management
- **Stop-loss & take-profit** – configured in pips via `StopLossPips` and `TakeProfitPips`.
- **Break-even** – when enabled, the stop is moved to the entry price plus `BreakEvenOffsetPips` after price advances by `BreakEvenTriggerPips`.
- **Trailing stop** – if `EnableTrailing` is true and unrealised profit exceeds `TrailingStopPips`, the stop trails price with step `TrailingStepPips`.

All price conversions use the instrument pip size to match MetaTrader behaviour.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `OrderVolume` | Order size used for market entries. | `1` |
| `CandleType` | Primary candle series used for signals. | `M15` |
| `BarsToCount` | Number of previous closes checked for exhaustion. | `10` |
| `FastMaLength` | Fast LWMA period. | `6` |
| `SlowMaLength` | Slow LWMA period. | `85` |
| `MomentumLength` | Momentum period on the higher timeframe. | `14` |
| `MomentumThreshold` | Minimum absolute deviation from 100 for momentum confirmation. | `0.3` |
| `StopLossPips` | Stop-loss distance in pips. | `20` |
| `TakeProfitPips` | Take-profit distance in pips. | `50` |
| `UseBreakEven` | Enable stop relocation to break-even. | `false` |
| `BreakEvenTriggerPips` | Profit in pips needed before moving the stop. | `30` |
| `BreakEvenOffsetPips` | Extra pips added when moving to break-even. | `30` |
| `EnableTrailing` | Activate trailing stop management. | `true` |
| `TrailingStopPips` | Profit in pips required to start trailing. | `40` |
| `TrailingStepPips` | Distance maintained by the trailing stop. | `40` |

## Notes
- The higher timeframe for momentum follows MetaTrader steps: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1, W1→MN1.
- MACD confirmation always uses the monthly timeframe (MN1).
- The strategy expects timeframe-based candle types; tick or range candles are not supported.
