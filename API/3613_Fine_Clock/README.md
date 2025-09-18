# Fine Clock Strategy

## Overview
- **Source**: Converted from the MetaTrader 5 expert advisor `FineClock.mq5` by Vladimir Gomonov (2009).
- **Purpose**: Decorative overlay that keeps a digital clock visible on the trading chart and inside the strategy comment.
- **Type**: Visual/utility strategy – does not submit any trading orders.

## How it works
1. Subscribes to the configured candle series to make sure a price reference is always available for chart drawing.
2. Subscribes to Level1 updates in order to anchor the label near the latest traded price (falls back to mid-price or the symbol's price step).
3. Starts the built-in strategy timer with a period that depends on the selected display format (seconds or minutes).
4. On every timer tick the strategy formats the chosen time source (local, server or UTC), mirrors the text into the strategy comment and draws one or two labels on the chart (second label mimics a drop shadow).

The implementation uses only StockSharp high-level helpers (`SubscribeCandles`, `SubscribeLevel1`, chart drawing extensions and the strategy timer). No manual indicator management or low-level message handling is required.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Candle data used for drawing and for determining the default time shift unit. | `TimeFrame(1s)` |
| `Format` | Clock output format (`Seconds` → `HH:mm:ss`, `Minutes` → `HH:mm`). | `Seconds` |
| `TimeSource` | Which clock to display: local machine, connector server time or UTC. | `Local` |
| `Corner` | Preferred chart corner that decides the direction of the horizontal/vertical offsets. | `RightLower` |
| `HorizontalOffset` | Additional horizontal shift expressed in candle units (negative values move left). | `0` |
| `VerticalOffset` | Additional vertical shift expressed in price steps (negative values move down). | `0` |
| `ShadowOffset` | Offset between the main text and the shadow text measured in the same units as the main offsets. | `1` |
| `UseShadow` | Enables or disables the secondary label that emulates the original drop shadow. | `true` |
| `ShowInComment` | When enabled, writes the same time string into the `Strategy.Comment` property. | `true` |

## Notes
- The chart label follows the latest known price; if no trades are available the strategy falls back to bid/ask midpoint or `Security.PriceStep`.
- The timer keeps the clock running even if the market is idle, closely mirroring the behaviour of the MetaTrader timer.
- Switching the display format immediately adjusts both the update frequency and the rendered text.
- Only label position and behaviour are translated; font face, size and colour are handled by the StockSharp UI theme.
- The strategy is purely informational and never calls any order registration methods.

## Differences from the MetaTrader version
- MetaTrader allowed pixel-level positioning, custom fonts and colours. StockSharp charts work in price/time coordinates, so offsets are specified in candle units and price steps.
- MetaTrader executed its logic inside `OnTimer`. This conversion uses the `Strategy.Timer` helper with the same cadence and integrates candle/Level1 subscriptions to keep the price anchor fresh.
- Two text objects are still used to emulate the drop shadow, preserving the visual style of the original expert advisor.
