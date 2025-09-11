# Стратегия Dynamic Breakout Master

Стратегия пробоя Donchian каналов с фильтрами по скользящим средним, RSI и ATR, а также с учётом объёма и времени торговли.

## Правила стратегии

- Long: цена пробивает верхнюю границу Donchian или откатывается после пробоя, MA1 > MA2, RSI между `RsiOversold` и `RsiOverbought`, ATR выше `AtrMultiplier`, объём выше среднего и внутри торговых часов.
- Short: цена пробивает нижнюю границу или откатывается после пробоя, MA1 < MA2, остальные условия аналогичны.
- Выход: стоп/трейлинг, тейк-профит, экстремумы RSI или пересечение скользящих.

## Параметры

- `DonchianPeriod` – период канала.
- `Ma1Length`, `Ma1IsEma` – первая скользящая.
- `Ma2Length`, `Ma2IsEma` – вторая скользящая.
- `RsiLength`, `RsiOverbought`, `RsiOversold` – фильтр RSI.
- `AtrLength`, `AtrMultiplier` – фильтр волатильности.
- `RiskPerTrade`, `RewardRatio`, `AccountSize` – управление размером позиции.
- `TradingStartHour`, `TradingEndHour` – торговые часы.
- `CandleType` – таймфрейм свечей.
