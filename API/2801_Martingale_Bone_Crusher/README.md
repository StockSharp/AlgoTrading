# Martingale Bone Crusher Strategy

## Overview

The **Martingale Bone Crusher Strategy** replicates the behaviour of the original MetaTrader expert advisor. The strategy trades in the direction of a fast/slow moving average comparison and applies a martingale money management model that increases the order size after a losing trade. A large set of risk-management tools is available, including fixed money targets, percentage targets, a configurable breakeven move, classic stop-loss/take-profit levels measured in price steps, and a profit-protection trailing stop measured in money.

## Trading Logic

- **Signal generation** – two simple moving averages are calculated on the primary candle series. When the fast average is below the slow average, the strategy seeks long entries. When it is above, the strategy seeks short entries. No new trades are placed while there is an active position.
- **Martingale sequencing** – after each completed trade the next position size is updated. If the last trade closed with a loss the next volume is either multiplied or incremented (depending on settings). Winning trades reset the position size back to the initial value.
- **Mode selection** – two martingale variants are provided:
  - `Martingale1`: the next trade always follows the current moving average direction, even after a loss.
  - `Martingale2`: after a loss the next trade is reversed relative to the direction that lost. This mirrors the behaviour of the original Expert Advisor’s second option.
- **Risk controls** – while a position is open the strategy continuously evaluates:
  - classical stop-loss and take-profit levels expressed in price steps;
  - an optional trailing stop that follows the extreme price with a fixed step distance;
  - a breakeven move that shifts the exit level after the position moves in favour by a configurable distance;
  - global money-based and percentage-based profit targets that close the position when the aggregated floating PnL exceeds the thresholds;
  - an additional trailing stop in money that locks in accumulated profit once the floating gain reaches the activation level.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `UseTakeProfitMoney` | Enables a fixed money take-profit target.
| `TakeProfitMoney` | Money amount that closes the trade when `UseTakeProfitMoney` is active.
| `UseTakeProfitPercent` | Enables a profit target expressed as a percentage of the initial portfolio value.
| `TakeProfitPercent` | Percentage used when `UseTakeProfitPercent` is enabled.
| `EnableTrailing` | Enables the money-based trailing stop.
| `TrailingTakeProfitMoney` | Floating profit required to arm the money trailing stop.
| `TrailingStopMoney` | Allowed drawdown from the peak floating profit after the trailing stop is active.
| `MartingaleMode` | Selects between `Martingale1` and `Martingale2` behaviour.
| `UseMoveToBreakeven` | Enables the breakeven stop adjustment.
| `MoveToBreakevenTrigger` | Price steps that the trade must move in favour before breakeven protection is activated.
| `BreakevenOffset` | Distance added to the entry price when the breakeven stop is placed.
| `Multiply` | Multiplier applied to the next volume after a loss when `DoubleLotSize` is `true`.
| `InitialVolume` | Base order volume used for the first trade and after wins.
| `DoubleLotSize` | Switches between multiplicative (`true`) and additive (`false`) martingale sizing.
| `LotSizeIncrement` | Volume increment applied after a loss when `DoubleLotSize` is `false`.
| `TrailingStopSteps` | Trailing-stop distance in price steps.
| `StopLossSteps` | Classic stop-loss distance in price steps.
| `TakeProfitSteps` | Classic take-profit distance in price steps.
| `FastPeriod` | Period of the fast simple moving average.
| `SlowPeriod` | Period of the slow simple moving average.
| `CandleType` | Candle series used for all indicator calculations.

## Notes

- Position volume is aligned with the instrument’s volume step, minimum, and maximum limits.
- Floating profit calculations rely on the instrument’s `PriceStep` and `StepPrice`. If they are zero the money-based protections are automatically skipped.
- Only the C# implementation is supplied. The Python port is intentionally omitted per task requirements.
