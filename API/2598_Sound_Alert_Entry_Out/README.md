# Sound Alert Entry Out
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy mirrors the original MetaTrader expert that plays a sound when a position is closed. It listens for filled trades that reduce or flip the current position, plays the selected terminal sound and, if enabled, writes a detailed notification message describing the exit.

## Details

- **Entry Criteria**: None. The strategy never sends orders and can run alongside other strategies or manual trading.
- **Long/Short**: Works for both long and short positions because it reacts to any closing trade.
- **Exit Criteria**: A trade whose direction is opposite to the previous position triggers the alert sequence.
- **Stops**: No. Risk management is delegated to the strategy or trader generating the trades.
- **Default Values**:
  - `Sound` = NotificationSound.Alert2
  - `NotificationEnabled` = false
- **Alerts**:
  - The terminal plays the chosen `.wav` file when the exit is detected.
  - When `NotificationEnabled` is true the strategy also logs a message with the trade id, side, volume, symbol and realized profit difference captured after the fill.
  - Supported sounds: `alert`, `alert2`, `connect`, `disconnect`, `email`, `expert`, `news`, `ok`, `request`, `stops`, `tick`, `timeout`, `wait`.
- **Usage Notes**:
  - Attach to any portfolio/security combination that produces trades; the logic observes account trades rather than indicator data.
  - Profit reported in the notification is computed as the difference between the current and previous strategy PnL after the trade is processed.
  - Because the strategy does not subscribe to market data it can run with zero candle or indicator bindings.

