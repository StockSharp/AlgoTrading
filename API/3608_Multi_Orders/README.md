# Multi Orders Strategy

The **Multi Orders Strategy** is a faithful C# conversion of the MetaTrader 5 expert advisor `multi.mq5`. The original robot adds two on-chart buttons that open multiple market orders on demand while also running a lightweight auto-entry that reacts to tight spreads. This StockSharp port keeps the same behaviour using high-level level1 subscriptions and built-in risk controls.

## Trading Logic
- Subscribes to best bid/ask updates via `SubscribeLevel1()` and stores the latest quotes.
- Whenever the spread is below `SlippagePoints` (converted to instrument price steps) the strategy compares the mid-price with the current ask/bid:
  - If the midpoint prints above the ask, a buy market order is sent.
  - If the midpoint prints below the bid, a sell market order is sent.
- Manual batches mimic the MetaTrader buttons. Call `TriggerBuyBatch()` or `TriggerSellBatch()` from the UI, tests, or automation layer to queue a series of market orders that will be dispatched on the next quote update.
- Protective stops and take profits are managed through `StartProtection`, using the configured step distances.

## Position Sizing
- `RiskPercentage` determines the share of portfolio equity risked per trade. Equity is sourced from `Portfolio.CurrentValue`, falling back to `CurrentBalance` and `BeginValue` if required.
- Stop distance is derived from `StopLossPoints` and the instrument `StepPrice`. If either value is unavailable the strategy falls back to the `BaseVolume` parameter.
- Volumes respect exchange constraints by rounding to `VolumeStep`, enforcing `MinVolume`, and clamping against `MaxVolume` when provided.

## Usage Notes
- The strategy assumes a netting account. Multiple orders in the same direction increase the aggregate position rather than creating separate tickets.
- Manual batch triggers are ignored until the strategy is started, connected, and allowed to trade (`IsFormedAndOnlineAndAllowTrading`).
- Set `SlippagePoints` according to the instrument tick size. A value that is too small will block the automatic entries entirely.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `BuyOrdersCount` | Number of market buy orders sent by a batch trigger. | `5` |
| `SellOrdersCount` | Number of market sell orders sent by a batch trigger. | `5` |
| `RiskPercentage` | Portfolio percentage used for risk-based sizing. `0` disables the calculation. | `1` |
| `StopLossPoints` | Stop-loss distance in price steps, also used for risk sizing. | `200` |
| `TakeProfitPoints` | Take-profit distance in price steps. | `400` |
| `SlippagePoints` | Maximum spread (in price steps) tolerated before automatic entries can fire. | `3` |
| `BaseVolume` | Fallback order size when risk sizing cannot be derived. | `1` |

