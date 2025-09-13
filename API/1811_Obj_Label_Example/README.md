# Obj Label Example Strategy

This strategy showcases how to draw a text label on a chart using the high-level API of StockSharp.

## Parameters

- `Candle Type` - defines the candle data to subscribe for chart information.
- `Label text` - text string that will be rendered on the chart.
- `Price offset` - vertical offset from the candle close price to place the label.

## Logic

1. Subscribes to the specified candle type.
2. After the first finished candle is received, a chart area is created and candles are drawn.
3. A single text label is placed at the candle open time and at the close price shifted by the specified offset.

The strategy is for demonstration only and does not perform any trading operations.
