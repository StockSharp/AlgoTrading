# Friday Bond Short Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This time-based strategy opens a short position on a specified day and hour in Eastern Time and closes it on another day and hour. By default, it sells on Friday at 13:00 ET and buys back on Monday at 13:00 ET.

## Details

- **Entry Criteria:** Short on configured day and hour (ET).
- **Exit Criteria:** Close short on configured day and hour (ET).
- **Long/Short:** Short only.
- **Indicators:** None.
- **Stops:** None.
- **Timeframe:** 1 hour candles by default.
- **Category:** Time-based.
- **Complexity:** Low.
