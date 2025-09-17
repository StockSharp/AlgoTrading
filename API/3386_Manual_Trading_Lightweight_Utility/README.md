# Manual Trading Lightweight Utility Strategy

## Overview
The original "Manual Trading Lightweight Utility" expert advisor is a compact MetaTrader panel that exposes buttons for switching between market, limit, and stop orders, adjusts volumes independently for buy and sell actions, and automatically attaches stop-loss and take-profit offsets. This C# port recreates the same workflow inside StockSharp by representing every panel button as a strategy parameter. The strategy does not produce autonomous signals; it waits for your manual instructions and then executes the requested action using the high-level API while supervising protective exits.

## Recreated functionality
- **One-shot buy and sell requests.** Two boolean toggles emulate the panel buttons. Setting `BuyRequest` or `SellRequest` to `true` triggers exactly one market, limit, or stop order based on the selected mode and immediately resets the toggle to `false`.
- **Automatic or manual pending prices.** Each side can either reuse the MetaTrader offsets (`LimitOrderPoints` and `StopOrderPoints`) or accept a manual absolute price. Automatic pricing uses the current best bid/ask or the latest candle close when quotes are unavailable.
- **Independent volumes.** You can share one default volume between both sides or activate per-side volumes to mirror the Lot Control switch from the MQL version.
- **Point-based protection.** `TakeProfitPoints` and `StopLossPoints` translate the MetaTrader point distances into price offsets using the instrument `PriceStep`. The strategy monitors completed candles and closes the position with a market order when a protective level is pierced.
- **Comment feedback.** Every manual action writes a log entry that includes the configured `OrderComment`, making it easy to follow the executed commands without a visual panel.

## Strategy flow
1. The strategy subscribes to the candle type selected by `CandleType`. Finished candles provide the reference prices used for offsets and risk supervision.
2. For every completed candle the strategy:
   - Updates the base class `Volume` with `DefaultVolume` (useful for visual inspection in StockSharp).
   - Detects changes in `BuyRequest` and `SellRequest` and marks them as pending actions.
   - Once market data is ready (`IsFormedAndOnlineAndAllowTrading()`), executes the requested actions, resolves prices for pending orders, and logs the result.
   - Calls the risk manager that records the entry price whenever the net position changes and issues market exits if stop-loss or take-profit thresholds are crossed.
3. When the position returns to flat, all internal state is reset so the next manual request starts with a clean slate.

## Parameters
- **`CandleType`** – market data series used for price references and risk management.
- **`BuyOrderMode` / `SellOrderMode`** – choose between `MarketExecution`, `PendingLimit`, or `PendingStop` for each side.
- **`UseAutomaticBuyPrice` / `UseAutomaticSellPrice`** – enable automatic offset pricing. Disable to supply a fixed absolute price.
- **`BuyManualPrice` / `SellManualPrice`** – manual pending order prices applied when automatic pricing is off (set to `0` to ignore).
- **`DefaultVolume`** – shared order volume when individual volumes are disabled.
- **`UseIndividualVolumes`** – toggles the Lot Control analogue. When enabled the next two parameters override the shared volume.
- **`BuyVolume` / `SellVolume`** – per-side volumes.
- **`TakeProfitPoints` / `StopLossPoints`** – protective distances expressed in MetaTrader points. Zero disables the respective feature.
- **`LimitOrderPoints` / `StopOrderPoints`** – offsets applied to automatic limit and stop prices, also measured in points.
- **`BuyRequest` / `SellRequest`** – momentary toggles that emulate the panel buttons. They are automatically reset after the request is processed.
- **`OrderComment`** – free-form text appended to the log when an action is executed.

## Usage guidelines
1. Configure `CandleType` to match the granularity you want to use for offsets and risk checks. The default one-minute timeframe resembles the tick-driven behaviour of the MetaTrader script while staying compatible with historical backtests.
2. Choose whether to work with a single `DefaultVolume` or enable `UseIndividualVolumes` to control buy and sell volumes separately. Volumes must remain positive.
3. Decide how pending prices should be calculated. Leave `UseAutomatic*Price` enabled to replicate the MetaTrader point offsets or disable it and provide `BuyManualPrice` / `SellManualPrice` values explicitly.
4. Set `TakeProfitPoints` and `StopLossPoints` as required. When they are greater than zero, the strategy converts them to price distances using the instrument `PriceStep` and closes the position with a market order as soon as a candle crosses the relevant threshold. If the symbol lacks a configured `PriceStep`, a warning is logged and protective distances are skipped.
5. To submit an order, change `BuyRequest` or `SellRequest` from `false` to `true`. The strategy resolves the request on the next finished candle, sends the chosen order type, writes a log entry, and resets the flag so the action is not repeated automatically.
6. Reissue any action by toggling the corresponding parameter again. Requests remain idle if the required price cannot be resolved (for example because a manual price is zero); fix the configuration and re-toggle to try again.

## Differences from the original MQL utility
- The MetaTrader chart objects are replaced with StockSharp parameters. Every button and toggle from the original panel is now an editable property that can be controlled from the UI or via automation scripts.
- Protective levels are executed with market orders when breached instead of registering separate stop/limit protective orders. This keeps the implementation within the high-level API and avoids managing order lifecycles manually.
- Automatic prices fall back to the latest candle close if best bid/ask quotes are not available, ensuring deterministic behaviour during backtests where order book data might be absent.

## Notes
- The strategy stores the entry price whenever the net position changes. If you scale into a trade, the protective offsets re-anchor on the candle close that reflects the new size.
- Spread compensation is included in the stop-loss calculation by adding the best known spread (or one price step when quotes are missing) to the configured point distance, mirroring the MQL logic that widened sell stops by the current spread.
- Log entries contain the configured comment, order type, price (for pending orders), and volume, providing a concise audit trail for each manual action.
