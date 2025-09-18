# Chart Button Test Strategy (StockSharp Conversion)

## Overview
The **Chart Button Test Strategy** is a direct StockSharp conversion of the original MetaTrader script `Chartbuttontest.mq4` (44020). The MQL program showcased a draggable chart button that updated its label with the current price while the user moved it across the chart. In the StockSharp environment we replicate the same concept by drawing a rectangle and a text label that follow market data and emit informative log messages when their position changes. This sample is a visual helper and does not submit trading orders.

## Core Ideas
* Use the high-level `Strategy` API with candle subscriptions only.
* Emulate the button body by drawing four lines that outline a rectangle.
* Display the live price inside a text label that mirrors the button caption from the MQL version.
* Provide configuration parameters to control the horizontal anchor, vertical height and text caption.
* Keep the code free from manual collections or indicator lookups and rely on strategy state.

## Runtime Behaviour
1. **Initialization**
   * The first finished candle defines the initial anchor time and the price used for the button bottom edge. The strategy tries to use the best ask or last price first and falls back to the candle close.
   * A dedicated chart area is created. The original candles are shown alongside the synthetic button lines and label.
   * A log entry confirms the creation coordinates.
2. **Ongoing updates**
   * Each finished candle refreshes the stored open time queue. The oldest value is removed once the queue length exceeds the configurable `LookbackCandles` parameter.
   * When horizontal movement is unlocked, the left side of the rectangle slides through time following the oldest entry inside the queue. When locked, the rectangle stretches only vertically.
   * The bottom edge uses the current candle close. The top edge stays `PriceHeight` units above the bottom edge.
   * After redrawing the rectangle and caption the strategy logs the new price and time, replicating the `EVENT_END_DRAG` notifications from the MQL script.
3. **No trading**
   * The strategy intentionally avoids orders. It is meant purely as an interface demonstration and a convenient example on how to migrate graphical MQL helpers to StockSharp.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| **Candle Type** | Candle data type used for the subscription. | 1 minute time frame |
| **Lookback Candles** | Number of finished candles stored to determine the left anchor. | 100 |
| **Button Height** | Distance (in price units) between the rectangle bottom and top edges. | 0.001 |
| **Button Text** | Caption rendered next to the rectangle. | `Button price:` |
| **Lock Horizontal Movement** | When `true`, the left time anchor remains fixed. When `false`, it slides with the queue (like dragging the button horizontally). | `false` |

## Logging
The `AddInfoLog` messages mirror the original custom events:
* `Button created at <price> (time <timestamp>).`
* `Button moved to <price> at <timestamp>.`
These statements allow monitoring of every automatic “drag” triggered by incoming candles.

## Usage Tips
* Attach the strategy to any instrument where `PriceHeight` matches the tick size (adjust if the default value is too small or too large).
* Use the `Lock Horizontal Movement` parameter to simulate a fixed-time button while still allowing vertical updates.
* Combine the rectangle with other indicator outputs for visual dashboards or manual trading aids.

## Limitations Compared to MQL Version
* Manual mouse dragging is not supported. Instead, the anchor updates automatically when new candles arrive.
* The button height remains constant and is controlled only through the parameter.
* Because the strategy has no trade logic, it is intended for visualization and code reference only.
