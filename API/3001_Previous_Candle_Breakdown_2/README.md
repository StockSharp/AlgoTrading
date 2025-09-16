# Previous Candle Breakdown 2
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy that mirrors the MetaTrader expert "Previous Candle Breakdown 2". The algorithm watches the most recently
completed candle on a configurable timeframe and triggers trades when price pierces its high or low by a user-defined pip offset.
Optional moving average filtering, strict trading hours, position sizing by fixed volume or percentage risk, and layered
protective exits replicate the behaviour of the original MQL version inside StockSharp.

## Overview
- **Entry Logic**: Enter long when price exceeds the previous candle high plus an indent. Enter short when price breaks below the
  previous candle low minus the same indent.
- **Filters**: Optional fast/slow moving averages with shift parameters require directional confirmation before trading. Trading is
  also constrained to a start/end time window.
- **Position Sizing**: Choose between a fixed order volume or dynamic sizing based on portfolio value and stop-loss distance.
- **Risk Controls**: Static stop-loss and take-profit levels in pips, trailing stop with a step filter, and a global profit target
  that closes all positions.
- **Scaling**: The `MaxPositions` limit caps the absolute net position size for each direction.

## Default Parameters
- `IndentPips` = 10
- `FastPeriod` = 10, `FastShift` = 3, `SlowPeriod` = 30, `SlowShift` = 0, `MaMethod` = Simple
- `StopLossPips` = 50, `TakeProfitPips` = 150
- `TrailingStopPips` = 15, `TrailingStepPips` = 5
- `ProfitClose` = 100 (currency units of realised + unrealised PnL)
- `MaxPositions` = 10 (absolute contract/lot count per side)
- `OrderVolume` = 0 (disabled), `RiskPercent` = 5 (used when `OrderVolume` is zero and stop-loss is active)
- `StartTime` = 09:09, `EndTime` = 19:19
- `CandleType` = 4-hour time frame

## Trading Rules
1. Subscribe to the configured candle series and record each finished candle.
2. Check whether the current time falls inside the allowed trading session. If `ProfitClose` is reached, immediately flatten.
3. Compute breakout levels by adding/subtracting the pip indent from the previous candle’s high and low.
4. When price breaks those levels and MA conditions (if enabled) are satisfied, open trades respecting the `MaxPositions` limit.
5. Set initial stop-loss and take-profit distances from the entry price and activate trailing stops once price has moved in favour
   of the trade by at least the trailing distance plus step.
6. Continuously monitor candles: trigger stop-loss/take-profit exits when touched, trail stops as price advances, and reset
   protective levels once positions are closed.

## Notes
- Pip calculations adjust automatically for 3 or 5 decimal instruments to mimic MetaTrader’s point-to-pip conversion.
- When using percentage risk sizing, the algorithm estimates volume from current portfolio value and the configured stop-loss.
- The breakout check uses finished candles, so intra-bar spikes are evaluated at candle close/high/low levels.
- `MaxPositions` works with the strategy’s net position. If you use fractional volumes, the parameter represents the maximum
  absolute net size allowed per direction.
- Charts display candles, the active moving averages when enabled, and executed trades for visual confirmation.
