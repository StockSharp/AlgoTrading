# Negroni 开盘区间策略

根据可配置时间窗口计算的盘前或开盘区间突破进行交易。只在指定的交易时段内开仓，并在设定的收盘时间平仓。

## 参数
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
