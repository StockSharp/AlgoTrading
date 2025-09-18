# BreakOut15 Strategy

## Overview
BreakOut15 is a 15-minute breakout strategy converted from the MetaTrader 4 expert advisor "BreakOut15.mq4". The strategy combines a moving-average crossover filter with breakout execution and multi-stage trailing protection. Orders are sent through the high-level StockSharp API and rely on finished candles only.

## Core Logic
1. Calculate two configurable moving averages (fast and slow) using the selected method, period, shift, and applied price.
2. When the fast average crosses above the slow average, schedule a long breakout price at `Close + BreakoutLevel * PriceStep`. A bearish crossover schedules a short breakout at `Close - BreakoutLevel * PriceStep`.
3. Pending breakout prices are cancelled if the crossover condition disappears, trading hours end, or a breakout in the opposite direction becomes active.
4. Market entries are executed once the candle breaks through the pending level and equity and risk checks pass.
5. Open positions are managed by stop-loss, take-profit, and one of three trailing-stop modes. Moving-average crossbacks force an immediate exit.
6. Optional time filters prevent new trades outside the configured window and can liquidate positions late on Fridays.

## Money Management
* **UseMoneyManagement / TradeSizePercent** – enables risk-based sizing. The position size equals the integer part of `floor(equity * percent / 10000) / 10`, with a minimum of 1 lot.
* **FixedVolume** – fallback size when money management is disabled or equity is unavailable.
* **MaxVolume** – caps any computed volume.
* **MinimumEquity** – blocks new trades when equity drops below the threshold.

## Risk Management
* **StopLossPips / TakeProfitPips** – classic protective offsets measured in pips (converted via the instrument price step).
* **UseTrailingStop** – turns on dynamic stop handling once a position exists.
* **TrailingStopType**
  * `Immediate`: trail by the original stop-loss distance right away.
  * `Delayed`: wait for `TrailingStopPips` of profit before trailing at that distance.
  * `MultiLevel`: lock in gains at three programmable milestones (`Level1/2/3TriggerPips`) and then trail by `Level3TrailingPips`.

## Trading Schedule
* **UseTimeLimit, StartHour, StopHour** – allow trading only inside the specified hour interval.
* **UseFridayClose, FridayCloseHour** – optionally flatten all positions late on Friday.

## Indicators and Data
* **Fast/Slow moving averages** – choose between Simple, Exponential, Smoothed, Linear Weighted, or Least Squares methods.
* **Applied price modes** – reproduce MT4 price sources (close, open, high, low, median, typical, weighted).
* **CandleType** – defaults to 15-minute time-frame candles but can be changed if needed.

## Additional Notes
* The strategy automatically synchronises entry, stop, and target prices with the current average position price so trailing adjustments reflect actual fills.
* All calculations depend on the instrument `PriceStep`; ensure it matches the traded market.
* Tests should validate breakout triggering, trailing-stop transitions, and money-management rounding rules across bullish and bearish scenarios.
