# Timer EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader TimerEA robot. It focuses on opening and closing trades at scheduled datetimes
with optional pending orders, trailing protection and break-even handling.

## Trading Logic

- **Schedule**
  - `OpenTime` triggers order placement once the first finished candle reaches the configured minute.
  - `CloseTime` forces position liquidation and optionally cancels remaining pending orders.
- **Order Modes**
  - Market, stop or limit entries can be selected. Pending orders are placed at a configurable distance (in price steps) and may
    expire after the specified number of minutes.
- **Direction Control**
  - Separate switches allow enabling long and/or short trades. Each side submits one order per run.
- **Risk Management**
  - Fixed volume or balance-based sizing (using `RiskFactor`) mimics the original lot selection.
  - Stop-loss and take-profit distances are expressed in price steps and recreated after each entry.
  - Trailing stop logic keeps the stop at a constant offset once profit exceeds the `BreakEvenSteps` buffer. The trail activates
    only when the stop is already beyond the initial offset plus the `TrailingStep`.
- **Protections**
  - Optional break-even requirement prevents trailing until the minimum profit threshold is achieved.
  - Pending orders that outlive their expiration are cancelled automatically.

## Default Parameters

- Order mode: Market.
- Open buy / sell: disabled.
- Take profit / stop loss: 10 steps each.
- Trailing stop and break-even: disabled.
- Pending distance: 10 steps with 60 minutes expiration.
- Lot sizing: Manual volume = 1.0 (risk factor = 1.0 for balance mode).
- Candle type: 1-minute time frame.

## Notes

- The strategy operates on finished candles and therefore reacts with up to one bar of latency.
- StockSharp uses a netted position model, so simultaneous long and short exposure is not supported even if both toggles are
  enabled.
- Price steps are calculated with `Security.PriceStep`. Instruments without a configured step will treat distances as raw price
  points.
