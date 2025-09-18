# Demo Resource EA (MQL #293)

## Overview

Demo Resource EA is a visual-only translation of the original MetaTrader example that demonstrates how to load bitmap resources into an on-chart button. The StockSharp version mimics this behaviour by plotting a configurable currency icon on the chart using text drawing tools. No trading actions are generated; the strategy exists purely to showcase chart annotation techniques.

When the strategy starts it subscribes to the configured candle series, creates a chart area, and waits for the first completed candle. Once a candle closes, the strategy draws a euro symbol ("€") or a dollar symbol ("$") near the candle location, emulating the pressed and released button states from the MQL sample. Users can switch between the two icons at runtime through a boolean parameter.

## Strategy Logic

1. Subscribe to the selected candle type and build a chart area for visualization.
2. On the first finished candle, compute the target time and price by applying user-defined horizontal and vertical offsets.
3. Draw the chosen currency icon at the calculated coordinates using the built-in `DrawText` helper.
4. Monitor the icon selection parameter; if it is toggled later, redraw the label with the new symbol to simulate changing the button texture.

Because this is a showcase of resource loading rather than a trading strategy, the position remains untouched and no orders are sent.

## Parameters

| Name | Description |
| --- | --- |
| **Candle Type** | Data type of candles that provide the anchor point for the label. |
| **Price Offset** | Vertical distance, in price units, added to the candle close before drawing the icon. |
| **Time Offset** | Horizontal shift added to the candle open time to move the label left or right. |
| **Use Dollar Icon** | When enabled the dollar symbol is drawn; otherwise the euro symbol is used. |

## Usage Notes

- Offsets allow you to place the icon away from the candle body, imitating the x/y distance properties of the original bitmap label.
- The text rendering method repeats the draw call whenever the icon changes; this is expected and safe for charts.
- Since the strategy is non-trading, it can be started on any instrument solely for demonstration or UI prototyping.
