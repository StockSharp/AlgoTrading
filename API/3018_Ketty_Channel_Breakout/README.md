# Ketty Channel Breakout Strategy

## Overview
The Ketty Channel Breakout Strategy is a direct C# conversion of the original Ketty.mq5 expert advisor. It builds a short-term price channel during a configurable pre-market window and waits for the market to spike outside of that range. When a spike happens, the strategy places a stop order on the opposite side of the channel with optional stop-loss and take-profit protection, mirroring the pending order workflow implemented in the MQL5 script.

## Trading Logic
1. **Daily reset** – At the first candle of every trading day the strategy clears pending orders (and protection orders if no position is open) and resets channel statistics.
2. **Channel construction** – Between `ChannelStartHour:ChannelStartMinute` and `ChannelEndHour:ChannelEndMinute` the highest high and lowest low of the selected `CandleType` are tracked. The detected range represents the breakout channel for the rest of the day.
3. **Order prices** – The planned buy stop is `channelHigh + OrderPriceShiftPips`, while the planned sell stop is `channelLow - OrderPriceShiftPips`. The pip-to-price conversion matches the original robot: when the instrument has 3 or 5 decimal places, one pip equals ten price steps; otherwise a single price step is used.
4. **Signal detection** – Once the channel is available and the current time is between `PlacingStartHour` and `PlacingEndHour`, the most recent finished candle is inspected. A buy setup appears if the candle’s low breaks below the channel by at least `ChannelBreakthroughPips`. A sell setup appears when the candle’s high exceeds the channel by the same distance.
5. **Pending order management** – Only one pending order is active at any time. As soon as a signal is generated, the previous pending order (if any) is cancelled and the new stop order is registered. All pending orders are removed automatically after `PlacingEndHour`.
6. **Protection orders** – After the pending order is filled, the strategy immediately submits the matching protective stop (if `StopLossPips` is positive) and the profit target (if `TakeProfitPips` is positive). Those orders are cancelled when the position is fully closed.

## Parameters
- `EntryVolume` – default volume for new orders.
- `StopLossPips` – distance between the entry price and the protective stop order; set to zero to disable.
- `TakeProfitPips` – distance between the entry price and the take-profit order; set to zero to disable.
- `ChannelStartHour` / `ChannelStartMinute` – time of day when the channel calculation begins.
- `ChannelEndHour` / `ChannelEndMinute` – time of day when the channel calculation ends. The channel may span past midnight because the implementation normalises the time window.
- `PlacingStartHour` – hour of day when pending orders can start to appear.
- `PlacingEndHour` – hour of day after which all pending orders are cancelled.
- `ChannelBreakthroughPips` – breakout buffer that must be pierced by the latest candle before a stop order is armed.
- `OrderPriceShiftPips` – additional offset added to the channel border when placing the pending stop order.
- `VisualizeChannel` – when enabled the strategy draws two horizontal lines that represent the current channel on the chart.
- `CandleType` – timeframe used to build and monitor the channel.

## Additional Notes
- The strategy assumes the instrument trades continuously; if data is missing inside the channel window the system will wait for new candles before arming any orders.
- Protective orders are registered using separate stop/limit orders after the entry fills, because StockSharp does not attach SL/TP directly to pending orders the same way as MetaTrader.
- Make sure that `EntryVolume` matches the broker’s lot step and that the selected `CandleType` corresponds to a liquid timeframe (the original robot was designed for one-minute bars).
