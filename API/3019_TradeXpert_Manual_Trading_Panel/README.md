# TradeXpert Manual Trading Panel Strategy

## Overview
The original TradeXpert MQL5 expert advisor is a manually operated trading panel that exposes a collection of buttons for opening positions, placing pending orders, applying protective stops, and quickly reversing or closing an existing trade. This C# port reproduces the same toolkit inside StockSharp by turning every panel action into a strategy parameter. The strategy itself does not generate trading signals; instead it listens to your manual instructions, executes the requested orders, and supervises protective exits on the incoming candle flow.

## Recreated functionality
- **Market actions.** Single-use requests for `Buy` or `Sell` market orders using the configured trade volume.
- **Pending orders.** One-shot placement of Buy Limit/Stop and Sell Limit/Stop orders using an absolute price or an offset from the latest candle close.
- **Protective management.** Stop-loss and take-profit levels can be defined either as absolute price levels or as offsets from the recorded entry price. The strategy monitors candle extremes and closes the position with a market order when a protective level is breached.
- **Manual exit controls.** Dedicated parameters replicate the Close and Reverse buttons from the MQL panel, allowing you to close or flip a position on demand.

## Strategy logic
1. The strategy subscribes to the candle type specified by `CandleType`. The stream is used to determine the most recent closing price for offsets and to detect whether protective levels were crossed.
2. On every finished candle the strategy:
   - Applies the latest `TradeVolume` to the base class `Volume` property.
   - Handles manual close or reverse requests even if no indicators are formed yet.
   - Once market data is confirmed as ready, executes pending entry requests, registers pending orders, and evaluates stop-loss / take-profit triggers.
3. When a position size changes (new entry, scale in, or reduction), the strategy refreshes the stored entry price so that offset-based stops immediately reflect the latest trade.
4. Protective logic uses the candle high/low to identify breaches. When a level is crossed, a market order is sent in the opposite direction with the current absolute position size to ensure the position is fully closed.

## Parameters
- **`CandleType`** – candle series used to monitor prices for offsets and risk checks.
- **`TradeVolume`** – volume applied to every market and pending order (must be positive).
- **`EntryAction`** – momentary selector with values `None`, `BuyMarket`, or `SellMarket`. Setting a value different from `None` triggers the corresponding market order exactly once and then resets back to `None`.
- **`PendingAction`** – pending order selector (`None`, `BuyLimit`, `BuyStop`, `SellLimit`, `SellStop`). The action is consumed after a valid order is registered.
- **`PendingPrice`** – absolute price for the pending order. Leave at `0` to rely on `PendingOffset`.
- **`PendingOffset`** – offset applied to the most recent candle close when `PendingPrice` is zero. Positive offsets automatically adjust the price above/below the close depending on the selected action.
- **`UseStopLoss`** / **`StopLossPrice`** / **`StopLossOffset`** – enable and configure stop-loss protection. Offsets are measured from the stored entry price when the absolute price is not provided.
- **`UseTakeProfit`** / **`TakeProfitPrice`** / **`TakeProfitOffset`** – analogous settings for take-profit management.
- **`ClosePositionRequest`** – set to `true` to issue an immediate market exit for the entire position. The flag resets to `false` after the request is processed.
- **`ReversePositionRequest`** – set to `true` to flip the current exposure. The strategy closes the existing position and opens an opposite one using `ReverseVolume`, then resets the flag.
- **`ReverseVolume`** – volume of the new position established after a reversal. If you need the reverse size to match the existing position, set it equal to the current absolute position.

## Usage guidelines
1. Choose the candle aggregation (`CandleType`) that matches how you want to measure offsets and risk. The default 1-minute timeframe mirrors the original panel behaviour that reacted to incoming ticks.
2. Configure `TradeVolume` and optional protective levels (`StopLoss*`, `TakeProfit*`). You can freely switch between absolute levels and offsets; the offsets activate whenever the absolute value is left at zero.
3. For pending orders, decide whether you prefer a fixed price (`PendingPrice`) or an offset from the latest close (`PendingOffset`). The strategy recalculates the price at the moment the order is submitted.
4. Submit trade instructions by changing `EntryAction`, `PendingAction`, `ClosePositionRequest`, or `ReversePositionRequest`. Each parameter behaves like a button: once the request is executed the value automatically resets so the action is not repeated on the next candle.
5. The strategy keeps monitoring price action while a position is open. Whenever a stop-loss or take-profit threshold is crossed the position is closed with a market order; both protective triggers are disabled until the next entry to avoid duplicate orders.

## Differences from the original MQL version
- The visual panel is replaced with strategy parameters. Every button from the original UI is now exposed as a toggle or selector that can be edited from the StockSharp parameter grid or automation scripts.
- Instead of placing stop or limit orders for protection, the strategy closes the position with market orders when the specified price levels are breached. This keeps the implementation compatible with the high-level API and avoids maintaining separate stop orders.
- Price offsets use finished candles instead of raw ticks. This keeps behaviour deterministic across backtests and live trading sessions while still delivering intraday responsiveness.

## Notes
- You can queue multiple instructions within the same candle (for example, request a market buy and immediately request a take-profit offset). The strategy processes them sequentially on the next finished candle.
- If you need to reissue the same action, simply select the desired value again; the internal tracking logic detects the change and executes the new request.
- When scaling into a position, the stored entry price is updated to the close of the candle that reflects the new size. Adjust the offsets accordingly if you require precise protective distances.
