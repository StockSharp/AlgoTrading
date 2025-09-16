# Send Close Order Strategy

Send Close Order is a port of the 2009 MetaTrader 4 expert advisor "SendCloseOrder" by Vladimir Hlystov. The original script draws four manual trendlines based on Bill Williams fractals and opens or closes market orders whenever price touches one of those projected levels. The StockSharp version replicates the decision logic with fully automated line management and works on any candle series provided by the platform.

## Trading logic

1. **Fractal detection** – every finished candle updates a five-bar sliding window. Once the window is full, the candle in the middle is checked against the Bill Williams fractal conditions. Confirmed highs and lows are stored chronologically.
2. **Trendline reconstruction**
   - *Sell line* connects the latest two upward fractals that are separated by a downward fractal, forming a resistance slope.
   - *Close #1* is the sell line shifted upward by `15` price steps (15 × `Security.PriceStep`) and acts as the long exit rail.
   - *Buy line* connects the latest two downward fractals that are separated by an upward fractal, forming a support slope.
   - *Close #2* is the buy line shifted downward by `15` price steps and acts as the short exit rail.
3. **Signal evaluation** – the four lines are extrapolated to the timestamp of the finished candle. If the projected price lies inside the candle’s high/low range (with a small tolerance of two price steps), the corresponding action is triggered.
4. **Order management**
   - Touching Close #1 or Close #2 immediately closes the entire position via `ClosePosition()`.
   - Touching the Sell or Buy line opens a market order with volume `TradeVolume`, provided that the resulting absolute position does not exceed `MaxOrders × TradeVolume`. When an opposite position exists, the order offsets it first and then stacks a new entry, mirroring the behaviour of hedging accounts.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `EnableSellLine` | `true` | Allow trades when the projected resistance line is hit. |
| `EnableBuyLine` | `true` | Allow trades when the projected support line is hit. |
| `EnableCloseLongLine` | `true` | Allow closing long positions on the shifted resistance line (Close #1). |
| `EnableCloseShortLine` | `true` | Allow closing short positions on the shifted support line (Close #2). |
| `MaxOrders` | `1` | Maximum number of stacked entries in the current direction. |
| `TradeVolume` | `0.1` | Volume of each individual market order. |
| `CandleType` | `1h` time frame | Candle series used for fractal calculations. |

## Differences versus the MetaTrader version

- The StockSharp port recalculates the four lines every time a new fractal appears. In MetaTrader the user had to delete and redraw trendlines manually.
- Execution is based on aggregated net positions; simultaneous long and short baskets are not supported by StockSharp’s default portfolio model.
- Touch detection uses the high/low of the finished candle with a price-step tolerance instead of the instantaneous Bid/Ask quotes from ticks.
- Chart objects (trendlines and labels) are not created; the focus is on trading signals.

## Usage notes

- The strategy can run on any instrument that provides candles and a valid `PriceStep`. When `Security.PriceStep` is zero the code falls back to `0.0001`.
- Increase `MaxOrders` to emulate the stacking behaviour of the original EA. Keep `TradeVolume` aligned with the instrument’s lot size to avoid rounding.
- The line offset is fixed to the historical value of 15 points. Adjust the source code if the MetaTrader input is modified.

Only the C# implementation is provided. A Python translation will be added separately if required.
