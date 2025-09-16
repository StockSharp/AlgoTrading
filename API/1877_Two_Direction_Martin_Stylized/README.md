# Two Direction Martin Stylized Strategy

This strategy implements a simplified two-way martingale approach. On start it opens both long and short positions and places limit orders to capture profits at a configurable distance.

## How it works
1. Calculates the spread and sets the take-profit distance as a percentage of the current ask price.
2. Sends an initial sell market order with a buy limit target below the bid and a buy market order with a sell limit target above the ask.
3. When one of the limit orders is missing or price moves outside the predefined range, the algorithm recalculates volumes using `Same Side %` and replaces pending orders. Additional market orders are sent to balance the position if required.
4. All orders are split into pieces that do not exceed the `Volume Limit` parameter.

## Parameters
- **Take Profit %** – distance for profit targets from current price.
- **Base Volume** – minimal volume for each initial order.
- **Volume Limit** – maximal volume for a single order piece.
- **Same Side %** – percent of total volume allocated to the dominant side.
- **Candle Type** – candle subscription used as a time driver.
