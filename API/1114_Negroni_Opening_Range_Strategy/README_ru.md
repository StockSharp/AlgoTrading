# Стратегия Negroni Opening Range

Торговля пробоями диапазона, рассчитанного по диапазону премаркета или первой сессии. Сделки открываются только внутри заданного торгового окна и все позиции закрываются в конце сессии.

## Параметры
- `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- `MaxTradesPerDay` = 3
- `Direction` = TradeDirection.LongShort
- `SessionStart` = new TimeSpan(9, 30, 0)
- `SessionEnd` = new TimeSpan(14, 0, 0)
- `CloseTime` = new TimeSpan(16, 0, 0)
- `UsePreMarketRange` = true
- `PreMarketStart` = new TimeSpan(8, 0, 0)
- `PreMarketEnd` = new TimeSpan(9, 0, 0)
- `OpenRangeStart` = new TimeSpan(9, 5, 0)
- `OpenRangeEnd` = new TimeSpan(9, 30, 0)
