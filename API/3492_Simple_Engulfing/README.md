# Simple Engulfing Strategy

## Overview
The **Simple Engulfing Strategy** replicates the behaviour of the MetaTrader 4 experts "simple engulf mt4 buy" and "simple engulf mt4 sell". Both experts detect engulfing candlestick patterns and open trades in a single direction. The StockSharp port merges both advisors into one configurable strategy so that the trader can reproduce the original buy-only, sell-only or combined behaviour inside the StockSharp framework.

The strategy listens to completed candles only, which matches the bar-close execution style used by the MetaTrader version. All order placement uses the high-level StockSharp API (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`, and `StartProtection`) to stay close to the StockSharp coding guidelines.

## Trading Logic
1. Build candles based on the configured `CandleType`.
2. Wait for the current candle to finish and keep the previous completed candle in memory.
3. Calculate the current candle body size in pips. Reject the pattern when it is below `MinBodyPips` or above `MaxBodyPips` (if the maximum filter is enabled with a positive value).
4. Detect a **bullish engulfing** pattern when:
   - The previous candle is bearish (close below open).
   - The current candle is bullish (close above open).
   - The current open is below or equal to the previous close.
   - The current close is above or equal to the previous open.
5. Detect a **bearish engulfing** pattern using the mirrored conditions.
6. When a valid pattern appears, make sure automated trading is allowed (`IsFormedAndOnlineAndAllowTrading()`) and that the configured direction allows the trade:
   - `BuyOnly` replicates the "simple engulf mt4 buy" robot.
   - `SellOnly` replicates the "simple engulf mt4 sell" robot.
   - `Both` enables bi-directional trading.
7. Use the configured `TradeVolume` for every entry. If the strategy is currently positioned on the opposite side it closes the position and flips by adding the absolute position size to the entry order, matching the MetaTrader behaviour when switching from short to long (or vice versa).
8. Optional stop-loss and take-profit levels are applied through `StartProtection` using price-based units. They convert the pip distances into instrument price increments so that StockSharp manages protective orders in the same way as the original experts.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | `TimeFrame(15 minutes)` | Candle type and aggregation interval used to detect patterns. |
| `TradeVolume` | `0.01` | Order volume per entry, identical to the MetaTrader experts. |
| `StopLossPips` | `20` | Stop-loss distance expressed in pips. Set to `0` to disable the protective order. |
| `TakeProfitPips` | `20` | Take-profit distance expressed in pips. Set to `0` to disable the protective order. |
| `MinBodyPips` | `0` | Minimum candle body (in pips) required for a valid engulfing pattern. |
| `MaxBodyPips` | `50` | Maximum candle body (in pips) allowed for a valid engulfing pattern. Use `0` to remove the upper filter. |
| `Direction` | `BuyOnly` | Defines which side(s) of the original advisors should be executed (`BuyOnly`, `SellOnly`, or `Both`). |

## Practical Notes
- The pip size adapts to the traded instrument automatically by analysing the instrument's `PriceStep` and number of decimal places. This ensures the pip filters and protective orders behave like the MetaTrader inputs on both 4-digit and 5-digit forex symbols.
- Protective orders are sent only when `StopLossPips` or `TakeProfitPips` are positive. Otherwise, the strategy leaves exits to discretionary management or other automation modules.
- Because the strategy waits for fully finished candles, signals are generated at the close of each bar, avoiding intra-bar repainting.
- High-level API calls keep the implementation concise and follow the project guideline of preferring ready-made StockSharp components over manual order handling.

## Differences from the Original
- Both MetaTrader advisors are combined into a single strategy with a `Direction` parameter instead of two separate files.
- Logging and charting helpers from StockSharp (optional candle and trade plots) are added for better visibility when running inside StockSharp terminals.
- Risk management uses StockSharp's `StartProtection` helper, which internally manages stop-loss and take-profit orders via the StockSharp engine. The resulting behaviour is equivalent to using hard stops in MetaTrader.
