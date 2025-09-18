# One Price Stop-Loss / Take-Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy replicates the MetaTrader script "One Price SL TP" inside StockSharp. Instead of opening trades, the algorithm watches the current position on the configured instrument and makes sure that both protective orders are aligned with a single target price specified by the user.

Whenever the parameter **`ZenPrice`** is above zero, the strategy compares it with the live bid/ask quotes:

- For a **long** position: if `ZenPrice` is higher than the ask, a take-profit limit order is placed at that price; if `ZenPrice` is lower than the bid, a stop-loss stop order is registered instead.
- For a **short** position: if `ZenPrice` is lower than the bid, it becomes the take-profit limit order; if `ZenPrice` is higher than the ask, it becomes the stop-loss stop order.

When the price falls between bid and ask nothing is sent, so the previous protective order remains untouched. As soon as the position is closed or the parameter is reset to zero, all protective orders are cancelled automatically.

## How it works

1. Subscribes to Level1 data to receive up-to-date bid/ask quotes that are required for the direction checks.
2. Keeps track of the current strategy position volume and direction. Positions are assumed to be created manually or by other strategies.
3. On each quote, position or personal trade update, recalculates which side of the market the `ZenPrice` belongs to and builds the corresponding protective order type.
4. Normalises the requested price using the instrument price step and rounds the order volume to exchange limits before sending anything to the trading connector.
5. Uses `ReRegisterOrder` to modify already active protective orders instead of cancelling them, matching the behaviour of MetaTrader's in-place modification.

## Parameter

- **`ZenPrice`** – absolute price that should be used either as a stop-loss or take-profit level. Set the value to `0` to disable the automation. Default: `0`.

## Practical notes

- The strategy never submits entry orders. It is safe to start it alongside discretionary trading terminals or other automated strategies.
- Protective orders are issued only after the first Level1 snapshot delivers both bid and ask quotes. Until then the script waits, just like the original MQL version relied on the terminal quotes.
- When only one side of the market satisfies the condition (for example, `ZenPrice` is above ask but not below bid), the other protective order is cancelled to avoid stale prices.
- All comments inside the code are in English, while this documentation is provided in multiple languages in accordance with the project guidelines.

## Differences from the MetaTrader script

- The original script modifies the stop-loss and take-profit fields of an existing position ticket. StockSharp exposes protective orders as explicit stop and limit orders, therefore the conversion operates on exchange-visible orders instead.
- MetaTrader automatically snaps the price to the broker precision. In this port the same behaviour is reproduced via `NormalizePrice`, which leverages the symbol's price step and decimal settings.
- Position volume is rounded to exchange lot limits before sending the protective orders, ensuring compatibility with venues that require specific lot steps.
