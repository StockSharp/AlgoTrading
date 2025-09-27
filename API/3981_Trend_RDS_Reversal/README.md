# Trend RDS Strategy

## Overview
Trend RDS is a session-based reversal strategy originally written for MetaTrader. It scans for a three-bar momentum formation at the start of a specified trading window and fades the move by entering in the opposite direction. The StockSharp port keeps the original money management logic, including optional reversal of the signals, fixed stop-loss and take-profit levels, break-even protection, and a trailing stop with adjustable step size.

## Trading Logic
1. **Signal window** – At the configured `Start Time` the strategy inspects up to 100 recently closed candles.
2. **Pattern detection** – It looks for the first sequence of three consecutive bars where either:
   - Highs rise while lows rise (`High[n] < High[n+1] < High[n+2]` and `Low[n] > Low[n+1] > Low[n+2]`).
   - Highs fall while lows fall (`High[n] > High[n+1] > High[n+2]` and `Low[n] < Low[n+1] < Low[n+2]`).
   A symmetrical expansion in both directions is treated as a conflict and ignored. The signal direction is optionally reversed when `Reverse Signals` is enabled.
3. **Entries** – The strategy submits a market order with the configured `Trade Volume` if there is no open position. If the opposite position is still open, it is closed first.
4. **Forced exit window** – Between `Close Time` and fifteen minutes afterwards any residual position is liquidated.
5. **Protection** – Once the position is open the strategy registers:
   - A stop-loss and take-profit order at the requested pip distances.
   - A break-even trigger that moves the stop to the entry price after reaching `Break-Even (pips)`.
   - A trailing stop that keeps a distance of `Trailing Stop (pips)` from the current price and advances only after an additional `Trailing Step (pips)` move.

## Parameters
| Name | Description |
| ---- | ----------- |
| **Trade Volume** | Market order size expressed in lots or contracts. |
| **Stop Loss (pips)** | Distance to the protective stop. Set to zero to disable. |
| **Take Profit (pips)** | Distance to the profit target. Set to zero to disable. |
| **Start Time** | Time of day (exchange time) when the pattern search begins. |
| **Close Time** | Time of day (exchange time) when all open trades are closed within 15 minutes. |
| **Reverse Signals** | Inverts long and short entries. |
| **Trailing Stop (pips)** | Base trailing distance. Zero disables trailing. |
| **Trailing Step (pips)** | Extra movement needed before the trailing stop updates again. |
| **Break-Even (pips)** | Profit threshold to move the stop to the entry price. Zero disables the feature. |
| **Candle Type** | Candle series used for the analysis. |

## Practical Notes
- The strategy relies on the instrument price step to calculate pip distances. Make sure the security exposes a valid `PriceStep` or `MinPriceStep` value.
- Only finished candles are processed, so the signal can appear at most once per day per timeframe.
- Stop and take-profit orders are refreshed whenever the position size changes, ensuring that partial fills keep consistent protection.
- Trailing and break-even logic activates only while a position is open and a valid entry price is known.

## Files
- `CS/TrendRdsStrategy.cs` – StockSharp C# implementation of the strategy.
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.
