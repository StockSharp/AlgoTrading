# Стратегия Bollinger Bounce Reversal
[English](README.md) | [中文](README_cn.md)

Стратегия ищет развороты, когда цена возвращается от полос Боллинджера при подтверждении индикаторами MACD и объёмом. Система ограничивает количество входов пятью сделками в день и использует фиксированные проценты стоп‑лосса и тейк‑профита.

## Детали

- **Условия входа**:
  - Long: `Close[1] < LowerBand[1] && Close > LowerBand && MACD > Signal && Volume >= AvgVolume * VolumeFactor`
  - Short: `Close[1] > UpperBand[1] && Close < UpperBand && MACD < Signal && Volume >= AvgVolume * VolumeFactor`
- **Long/Short**: Оба направления
- **Стопы**: Процентный тейк‑профит и стоп‑лосс
- **Значения по умолчанию**:
  - `BollingerPeriod` = 20
  - `BbStdDev` = 2m
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `VolumePeriod` = 20
  - `VolumeFactor` = 1m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Разворот
  - Направление: Оба
  - Индикаторы: Полосы Боллинджера, MACD, Объём
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
