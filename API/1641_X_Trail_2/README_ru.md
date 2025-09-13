# X Trail 2
[English](README.md) | [中文](README_cn.md)

Стратегия торгует по пересечению двух настраиваемых скользящих средних, рассчитанных из выбранного типа цены.

## Детали
- **Вход**: Покупка при пересечении MA1 выше MA2, подтверждённом двумя предыдущими барами; продажа при обратном сигнале.
- **Выход**: Обратное пересечение.
- **Индикаторы**: Две скользящие средние с выбором типа (simple, exponential, smoothed, weighted) и источника цены (close, open, high, low, median, typical, weighted).
- **Параметры**:
  - `Ma1Length` = 1
  - `Ma1Type` = MovingAverageTypeEnum.Simple
  - `Ma1PriceType` = AppliedPriceType.Median
  - `Ma2Length` = 14
  - `Ma2Type` = MovingAverageTypeEnum.Simple
  - `Ma2PriceType` = AppliedPriceType.Median
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
