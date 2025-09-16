# Limits Martin Strategy

This strategy places paired limit orders above and below the current market price. Each trade uses a configurable step distance and optional martingale position sizing to recover previous losses.

## Parameters
- **Step** – distance in pips between the market price and pending limit orders.
- **Stop Loss** – protective stop size in pips for open positions.
- **Take Profit** – target profit size in pips for open positions.
- **Use Martingale** – enables volume increase after a losing trade.
- **Loss Limit** – maximum number of consecutive losing trades before resetting volume.
- **Volume** – initial order volume.
- **Use MegaLot** – doubles the volume instead of adding the base volume when martingale is active.
- **Candle Type** – candle data type used for processing.

## Trading Logic
1. When there is no open position or active order, the strategy places a buy limit order below the last close and a sell limit order above it, both at the specified `Step` distance.
2. Upon execution of a position, the opposite pending order remains, allowing only one active position at a time.
3. The position is closed when either the stop loss or take profit level is reached.
4. After a losing trade, the position volume can be increased according to the martingale settings.

## Notes
- The strategy uses the high-level StockSharp API with the `Bind` approach for handling candle data.
- All comments within the code are written in English to meet repository conventions.
