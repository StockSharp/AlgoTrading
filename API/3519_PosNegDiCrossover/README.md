# PosNegDiCrossoverStrategy

## Overview
The **PosNegDiCrossoverStrategy** is a StockSharp port of the MetaTrader expert `_HPCS_PosNegDIsCrossOver_Mt4_EA_V01_WE`. The
original system listens to crossovers between the +DI and -DI lines of the Average Directional Index (ADX) and immediately
opens a position in the direction of the new leader. Each position is protected by symmetric stop-loss and take-profit
thresholds measured in pips, and losing trades trigger a martingale-style recovery loop that re-enters with a multiplied volume
until a fixed number of attempts is reached or a profitable exit occurs.

## Trading logic
1. **Signal detection** – when the finished candle delivers fresh ADX values, the strategy compares the current +DI and -DI
   readings with the previous ones. A bullish signal appears when +DI crosses above -DI, while a bearish signal is generated when
   +DI crosses below -DI. Only one initial entry per bar is allowed to mirror the MQL guard that prevented duplicate trades on
   the same candle.
2. **Time filter** – entries are allowed only inside a user-defined daily window. Outside the window the strategy keeps managing
   active positions (virtual stops and take profits) but does not open new cycles or continue a martingale sequence.
3. **Order placement** – a market order is sent in the detected direction with the configured base volume. After the fill the
   strategy converts `TakeProfitPips` and `StopLossPips` into absolute prices using the instrument step (a 10x multiplier is
   applied for instruments quoted with 3 or 5 decimals) and stores those levels for manual exit checks.
4. **Protection handling** – each finished candle is inspected: a long position is closed if the low pierces the stop or if the
   high reaches the target; short positions use the symmetrical conditions. Exits are executed with market orders so the cycle
   can evaluate the outcome before deciding the next step.
5. **Martingale loop** – after a loss the strategy multiplies the current volume by `MartingaleMultiplier`, increments the cycle
   counter and immediately re-enters in the same direction (respecting the trading window). When a profitable exit occurs or the
   number of attempts reaches `MartingaleCycleLimit`, the cycle resets to the base volume and waits for the next ADX crossover.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | 15-minute time frame | Candle series used for ADX calculations and stop/target monitoring. |
| `AdxPeriod` | 14 | Length of the Average Directional Index indicator. |
| `UseTimeFilter` | `true` | Enables the daily trading window. |
| `StartTime` | 00:00 | Beginning of the trading session (exchange time). |
| `StopTime` | 23:59 | End of the trading session (exchange time). |
| `OrderVolume` | 0.1 | Initial market order volume for each cycle. |
| `TakeProfitPips` | 10 | Distance to the profit target in pips (converted to price using the instrument step). |
| `StopLossPips` | 10 | Distance to the protective stop in pips. |
| `MartingaleMultiplier` | 2 | Volume multiplier applied after each losing trade during the martingale loop. |
| `MartingaleCycleLimit` | 5 | Maximum number of martingale re-entries allowed for the same signal. |

## Notes
- The strategy checks `IsFormedAndOnlineAndAllowTrading()` before sending any orders, ensuring proper initialisation and risk
  controls from the framework.
- Virtual stop-loss and take-profit handling mimics MetaTrader behaviour where protective orders are attached directly to the
  position. They are evaluated on finished candles to remain compatible with the high-level StockSharp API.
- When the trading window is disabled (either by parameter or by setting identical start and stop times) the strategy behaves as
  a 24/5 system, identical to the original expert with `is_start` and `is_stop` covering the full day.
