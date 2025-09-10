# Cci Threshold Strategy
[English](README.md) | [中文](README_cn.md)

Простая стратегия на основе индикатора CCI. Покупает, когда CCI опускается ниже порога, и закрывает позицию, когда цена закрытия превышает предыдущую.
Опциональные стоп-лосс и тейк-профит в пунктах.

## Детали

- **Условия входа**:
  - Long: `CCI < BuyThreshold`
- **Длинные/Короткие**: Только long
- **Условия выхода**:
  - `ClosePrice > предыдущего ClosePrice`
- **Стопы**: По желанию через `UseStopLoss` и `UseTakeProfit`
- **Значения по умолчанию**:
  - `LookbackPeriod` = 12
  - `BuyThreshold` = -90
  - `StopLossPoints` = 100m
  - `TakeProfitPoints` = 150m
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Фильтры**:
  - Категория: Mean reversion
  - Направление: Long
  - Индикаторы: CCI
  - Стопы: Опционально
  - Сложность: Низкая
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
