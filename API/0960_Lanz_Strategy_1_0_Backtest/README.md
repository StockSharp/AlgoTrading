# LANZ Strategy 1.0 [Backtest]
[Русский](README_ru.md) | [中文](README_cn.md)

LANZ Strategy 1.0 opens a single limit order each day based on the price direction between 08:00 and 18:00 New York time. The order is placed between 18:00 and 08:00 with fixed stop-loss and take-profit distances in pips. Any open position is closed at the configured New York hour.

## Parameters
- Candle Type
- Account Size USD
- Risk percent
- EP offset fraction
- Stop Loss (pips)
- Take Profit (pips)
- Manual close hour (NY)
- Enable buy
- Enable sell
