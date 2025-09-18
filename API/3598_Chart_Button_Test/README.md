# Chart Button Test Strategy

## Overview
The **Chart Button Test Strategy** recreates the idea of the original MQL sample by simulating a draggable chart button inside StockSharp. Instead of a visual widget, the strategy maintains a virtual price zone whose centre follows the latest market data. Informative log messages mirror the interaction flow that was previously provided by the manual MetaTrader button.

The strategy never submits orders. It can be attached to any instrument to monitor whether the closing price of finished candles is inside or outside of the tracked zone. Every transition is written to the strategy log, allowing the operator to connect alerts or further automation on top.

## How it works
1. When the first finished candle arrives, the strategy initialises the virtual button around the candle close price. The time window starts at the candle open time and extends by the configured selection length.
2. Each time a new candle closes the price zone is updated:
   * If **Lock Time** is disabled, the time window slides forward to the new candle.
   * Regardless of the lock setting, the zone top and bottom are recalculated from the latest price and the selected padding value.
3. Whenever the closing price crosses the zone boundary, an information log entry is produced stating whether the price entered or left the zone.
4. Changing parameters during execution immediately recalculates the virtual zone and produces explanatory log messages.

This behaviour reflects the MQL expert that allowed the user to drag a graphical button to mark a rectangular region on the chart. Here, the region is recalculated automatically based on incoming data while keeping the manual tuning options through parameters.

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Candle Type** | Candle data series processed by the strategy. Default is 5-minute time frame. |
| **Price Padding** | Half-width of the tracked zone around the centre price. Default is 10 price units. Must be positive. |
| **Selection Length** | Duration covered by the virtual button area, starting from the anchor time. Default is one hour. |
| **Lock Time** | When enabled, keeps the start and end time fixed after the initial candle. When disabled, the window follows every finished candle. |

## Usage
1. Add the strategy to a portfolio and assign the desired security.
2. Configure the candle type and zone size parameters to match the instrument volatility.
3. Start the strategy. After the first candle closes, watch the log messages to see how the zone is initialised.
4. Adjust **Price Padding** or **Selection Length** on the fly to move or resize the zone. The log will confirm every modification.
5. Monitor the logs for *entered* or *left* messages to detect when the market trades within or outside of the tracked area.

## Differences from the MQL version
- The StockSharp strategy does not create an on-chart GUI element. Instead, it simulates the button logic through internal calculations and log notifications.
- Mouse dragging is replaced by parameter updates, which keeps the implementation consistent with the StockSharp high-level API.
- The script never sends trading orders, mirroring the behaviour of the original example expert.

## Notes
- The strategy can be extended easily with alerts or with order execution when the price enters or leaves the zone.
- Because the logic relies on finished candles, choose a candle type that matches the responsiveness required for your workflow.
