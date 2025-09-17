# Trailing Stop When SL Used Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This module ports the MetaTrader expert `MQL/49021/Traling Stop (when SL used and Not).mq5` into the StockSharp high-level API. The original code continuously tightened an existing stop-loss and also created a trailing stop for positions that were opened without any protective orders.

## Original Idea
- **Scope**: Utility expert for MetaTrader 5 that manages stop-loss levels for all running positions.
- **Core behaviour**: For each buy order the script monitored the bid price. As soon as the market moved higher by the configured step it placed or advanced a stop-loss below the current bid. For short orders it mirrored the logic using the ask price.
- **Risk control**: The trailing only triggered after price moved beyond the entry price, so it never locked in a loss prematurely.

## StockSharp Adaptation
- **Netting model**: StockSharp strategies operate on a single aggregated position. The converter therefore maintains one protective order per direction and clears it whenever the position flips.
- **Point conversion**: MetaTrader multiplies the step parameter by `Point()`. The C# version multiplies the input by `Security.PriceStep` (falling back to `1` when unavailable) so the trailing distance stays instrument-aware.
- **Trade-driven updates**: The strategy subscribes to trade ticks through `SubscribeTrades().Bind(...)` to match the per-tick updates from the expert.
- **Stop handling**: When an update is required the previous stop order is cancelled and replaced with a new stop at the latest trailing level. This mirrors the `PositionModify` calls from the original code.

## Trading Logic
1. **Initialization**
   - Stores the price step and starts listening to trade ticks.
   - No automatic entries are submitted; the module only manages existing positions.
2. **Long position management**
   - Once the latest trade price is above the entry price, the strategy calculates `trailingLevel = price - step`.
   - If there is no active stop order, a new `SellStop` is registered. Otherwise, the stop is moved higher only when the new level exceeds the previous one.
3. **Short position management**
   - Uses the same procedure with `trailingLevel = price + step` and `BuyStop` orders.
4. **Position resets**
   - Closing the position cancels any active protective order and clears the stored trailing levels.

## Parameters
| Name | Description |
| --- | --- |
| **Trailing Step (points)** | Distance in points between trailing updates. Converted to price units using the instrument's price step. Must be greater than zero. |

## Practical Notes
- The original expert iterated over all terminal positions. To reproduce that behaviour launch this strategy after opening a trade or combine it with a signal strategy that issues entries.
- `PositionPrice` is used as the average entry price, ensuring the trailing distance is calculated from the true fill price even for partial fills.
- When the broker exposes asymmetric bid/ask prices, the last trade price remains a close approximation. Adjust the trailing distance if you require additional buffer.
- There are no automated unit tests for this conversion. Validate behaviour on a demo environment before trading live capital.
