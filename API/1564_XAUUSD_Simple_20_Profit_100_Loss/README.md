# XAUUSD Simple 20 Profit 100 Loss Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens a long position when no position is held and both cooldown timers are inactive.
It closes the position once unrealized profit reaches $20 or the loss hits $100.
After a profitable exit the strategy waits 15 minutes before re-entering, and after a losing exit it waits 12 hours.

## Parameters

- `ProfitTarget` – profit target in USD.
- `LossLimit` – maximum loss in USD.
- `TradeCooldown` – time to wait after a loss.
- `EntryCooldown` – time to wait after a profit.
- `CandleType` – candle timeframe.
