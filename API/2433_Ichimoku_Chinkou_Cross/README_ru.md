# Стратегия Ichimoku Chinkou Cross

Стратегия торгует по пересечению запаздывающей линии Ichimoku (Chinkou Span) с ценой.

## Логика стратегии

- **Покупка:** Chinkou пересекает цену снизу вверх, текущая цена и Chinkou находятся выше облака Kumo, а RSI выше `RsiBuyLevel`.
- **Продажа:** Chinkou пересекает цену сверху вниз, текущая цена и Chinkou ниже облака Kumo, а RSI ниже `RsiSellLevel`.

Стратегия использует защитный стоп через `StartProtection` и параметры Tenkan, Kijun, Senkou Span B и RSI.

## Параметры

| Имя | Описание | Значение |
|-----|----------|----------|
| `TenkanPeriod` | Период Tenkan-sen | 9 |
| `KijunPeriod` | Период Kijun-sen | 26 |
| `SenkouSpanPeriod` | Период Senkou Span B | 52 |
| `RsiPeriod` | Период расчёта RSI | 14 |
| `RsiBuyLevel` | Минимальный RSI для покупок | 70 |
| `RsiSellLevel` | Максимальный RSI для продаж | 30 |
| `StopLoss` | Процент или величина стоп-лосса | 2% |
| `CandleType` | Тип свечей | 5-минутные свечи |

## Индикаторы

- Ichimoku
- RSI
