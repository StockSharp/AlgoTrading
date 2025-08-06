# Option Expiration Week Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This Python strategy buys and holds an equity ETF only during option-expiration week. Starting on the Monday prior to the third Friday of each month the ETF is bought, and the position is closed at Friday's close. The idea exploits short-term strength often observed during expiration week.

Outside of this window the portfolio remains in cash. Daily candles are used and trades are sent as market orders once per day.

## Details

- **Instrument**: single equity ETF.
- **Signal**: calendar rule for the week ending on the third Friday.
- **Holding period**: Monday open to Friday close of expiration week.
- **Positioning**: fully invested during the window, flat otherwise.
- **Risk control**: trade skipped when order value below `MinTradeUsd`.
