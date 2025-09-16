# Spreader 2 Strategy

## Overview

The **Spreader 2 Strategy** is a pair-trading system converted from the MetaTrader expert advisor "Spreader 2". It watches two correlated instruments on a one-minute timeframe and looks for short-term deviations between their price movements. When both legs diverge within controlled volatility bounds while maintaining positive correlation, the strategy opens a market-neutral spread by going long one symbol and short the other. The combined position is closed when the total floating profit meets the configured target or when correlation rules are violated.

## Core Logic

1. Receive finished candles for the primary and secondary symbols and align them by close time.
2. Maintain rolling lists of closing prices so the algorithm can reference values that are `ShiftLength`, `2 * ShiftLength`, and `1440` bars in the past.
3. Compute first differences (`x1`, `x2` for the primary symbol and `y1`, `y2` for the secondary symbol) to detect local swings.
4. Skip trading when either instrument shows two consecutive moves in the same direction (trend filter) or when the products `x1 * y1` indicate negative correlation.
5. Evaluate the volatility ratio `a / b` where `a = |x1| + |x2|` and `b = |y1| + |y2|`. Only proceed when the ratio remains between `0.3` and `3.0`.
6. Scale the secondary leg volume proportionally to the volatility ratio and adjust it to the contract's volume step, minimum, and maximum values.
7. Confirm the intended trade direction with the 1440-bar (roughly one trading day) lookback. The spread is opened only when the daily move supports the shorter-term signal.
8. The strategy opens both legs simultaneously: the primary symbol trades with the configured `PrimaryVolume`, while the secondary symbol trades the adjusted size in the opposite direction.
9. While positions are open, the system continuously tracks floating profit of both legs. When the combined profit exceeds `TargetProfit`, it closes the spread and resets the entry references.
10. Safety checks automatically close orphaned positions if one leg exits unexpectedly and reopen missing legs when possible to keep the hedge balanced.

## Parameters

- **SecondSecurity** – secondary instrument participating in the spread. This parameter is required.
- **PrimaryVolume** – trade volume (in lots/contracts) for the primary symbol. Default is `1`.
- **TargetProfit** – absolute monetary profit target for the combined pair. Default is `100`.
- **ShiftLength** – number of candles between comparison points used in the first-difference calculations. Default is `30`.
- **CandleType** – data type used for candle subscriptions. By default the strategy works with one-minute time frame candles.

## Trading Rules

- Only finished candles are processed to avoid acting on incomplete data.
- Trend filters must show opposing moves over the last two `ShiftLength` windows for both symbols.
- Correlation must be positive, and the volatility ratio must remain in the `[0.3, 3.0]` band.
- The confirmation check against the 1440-bar lookback prevents trades that contradict the longer-term direction.
- Orders are sent with `OrderTypes.Market`. The secondary leg is registered explicitly with the secondary security and portfolio to mirror the MetaTrader behaviour.
- Open profit is computed using the latest candle closes and stored entry prices to determine when to exit the spread.

## Notes

- The strategy assumes both instruments share compatible contract specifications. If multipliers differ, trading is disabled and a warning is logged.
- Because the original algorithm relies on a full day of historical data, the StockSharp version also waits until at least 1440 candles are accumulated before the first entry.
- All risk management logic (profit target, orphaned-leg handling) is contained inside the strategy. Additional protections such as stop-losses can be added externally if required.
