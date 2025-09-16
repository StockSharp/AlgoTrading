# Amstell SL Averaging Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of the MetaTrader expert advisor `exp_Amstell-SL`. The system opens both a long and a short immediately, adds new orders every time price moves against the most recent entry by a fixed number of points, and relies on virtual (software-managed) take-profit and stop-loss levels to exit each ticket individually.

## Strategy Logic

- **Initial Entries**: When the strategy starts and there are no open trades, it submits one market buy (at the ask) and one market sell (at the bid).
- **Pyramiding on Drawdown**:
  - Long side: whenever the current ask is `ReentryPoints` (default 10 points) below the last long entry price, a new buy order of the same volume is sent.
  - Short side: whenever the current bid is `ReentryPoints` above the last short entry price, a new sell order of the same volume is opened.
- **Exit Rules (virtual management)**:
  - For every long ticket the strategy monitors the best bid and best ask. If the bid rises by `TakeProfitPoints` from the order price or the ask drops by `StopLossPoints`, the position is closed at market.
  - For every short ticket it checks whether the ask is lower by `TakeProfitPoints` or the bid is higher by `StopLossPoints`; in either case the sell order is covered at market.
- **Processing Order**: Exits are evaluated before any new entries, replicating the MetaTrader script that stops further actions after closing a position on the current tick.

## Parameters

- `TakeProfitPoints` – distance (in price steps) used to close profitable positions. Default: `30`.
- `StopLossPoints` – distance (in price steps) for protective exits. Default: `30`.
- `Volume` – lot size for each newly opened order. Default: `0.01`.
- `ReentryPoints` – adverse movement (in price steps) required to stack an additional order on the corresponding side. Default: `10`.

## Additional Notes

- The point value is derived from `Security.PriceStep`; if it is not provided by the exchange, a value of `1` is used.
- The strategy can be simultaneously long and short because it tracks buy and sell tickets independently, matching the hedging-style behaviour of the original expert advisor.
- Take-profit and stop-loss levels are executed virtually by market orders; they are not placed on the exchange order book.
- Risk increases quickly when price trends strongly in one direction because additional orders are opened without reducing previous exposure.
- Works best on symbols where the notion of "point" equals a minimal price increment, for example major forex pairs on MetaTrader-style pricing.
