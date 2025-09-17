# Auto Stop-Loss and Take-Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy automatically attaches protective stop-loss and take-profit orders to every open position on the configured instrument. It mirrors the behaviour of the original MetaTrader "AutoSet SL TP" expert by monitoring the active position list and enforcing broker distance restrictions before registering protective orders.

The strategy does not open trades on its own. Instead, it watches the volume, direction and execution price of positions that have been created manually or by other strategies. As soon as a long or short position appears, the algorithm calculates the desired stop-loss and take-profit levels expressed in MetaTrader-style pips, adjusts the levels to comply with the freeze and stop constraints published by the trading venue, and then submits the appropriate market-protective orders. When the position is fully closed the protective orders are cancelled automatically.

## How it works

1. Subscribes to Level1 data to receive best bid/ask prices together with optional `StopLevel` and `FreezeLevel` fields supplied by the broker.
2. Converts the configured pip distances into absolute prices using the symbol metadata (price step and decimal precision). Five-digit and three-digit quotes automatically scale by a factor of ten to match MetaTrader pip semantics.
3. On every quote update or personal trade notification:
   - Ignores the signal if there is no open position or if the direction does not match the configured filter (buy-only, sell-only or both).
   - Calculates the minimal permitted distance between the market price and a protective order. If the broker does not publish freeze/stop levels, the algorithm falls back to three spreads multiplied by 1.1 to stay safely outside of forbidden zones.
   - Determines the stop-loss price and take-profit price relative to the current ask (for longs) or bid (for shorts) and normalises the result to the instrument price step.
   - Places or re-registers stop or limit protective orders with the exact position volume. Orders are replaced only when the target price or volume changes, which keeps exchange modifications to a minimum.
4. If the position volume becomes zero, all outstanding protective orders are cancelled. The strategy also cancels the existing orders when the trade direction is no longer allowed by the filter.

Because the algorithm relies solely on external fills, it can be combined with discretionary trading, panels or other automated systems that manage entries, while this strategy guarantees a consistent protective envelope.

## Parameters

- **`StopLossPips`** – distance from the current market price to the stop-loss in MetaTrader pips. A value of `0` disables the stop order. Default: `50`.
- **`TakeProfitPips`** – distance from the current market price to the take-profit in MetaTrader pips. A value of `0` disables the take-profit order. Default: `140`.
- **`DirectionFilter`** – specifies which position direction is managed:
  - `Buy` – protect only long exposure.
  - `Sell` – protect only short exposure.
  - `BuySell` – protect both sides (default behaviour in the original script).

## Practical notes

- Protective orders are always created with the absolute position volume. If the broker enforces minimum or maximum lot sizes, the strategy rounds the volume to the nearest permissible value before placing the orders.
- The algorithm uses `ReRegisterOrder` to adjust active protective orders. This keeps the same exchange order identifiers whenever possible and avoids unnecessary cancellations.
- The fallback distance (spread × 3 × 1.1) prevents the stop or take-profit from violating hidden exchange restrictions when explicit freeze/stop levels are not provided.
- Since the strategy does not manage entries, it can be started before or after positions are opened. Any qualifying position that already exists at the time of startup will be protected immediately after the first quote update.
- MetaTrader "pips" differ from exchange price steps on symbols with three or five decimal digits. The strategy mirrors the original Expert Advisor by multiplying the point value accordingly, ensuring the configured numbers map exactly to the MT5 settings.

## Differences from the MetaTrader expert

- Instead of modifying in-position stop and take-profit attributes, StockSharp manages explicit protective stop and limit orders. This approach keeps the logic fully transparent inside the StockSharp order book.
- The StockSharp version uses Level1 market data to rebuild broker restriction levels. If the provider exposes different field names for freeze or stop distances, the strategy automatically discovers them through reflection on the `Level1Fields` enum.
- Every code comment and log message is in English to remain consistent with the coding guidelines, while the documentation is localised into Russian and Chinese for end users.
