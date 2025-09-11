# Стратегия MCOTs Intuition
[English](README.md) | [中文](README_cn.md)

Стратегия использует импульс RSI и его стандартное отклонение. Покупает при сильном, но ослабевающем положительном импульсе и продаёт при зеркальных условиях. Устанавливает фиксированную цель прибыли и стоп в тиках.

## Детали

- **Условия входа**:
  - Long: momentum > stdDev * multiplier и momentum < previousMomentum * exhaustionMultiplier
  - Short: momentum < -stdDev * multiplier и momentum > previousMomentum * exhaustionMultiplier
- **Направление**: Оба
- **Условия выхода**:
  - Фиксированная цель прибыли и стоп в тиках
- **Стопы**: Да
- **Значения по умолчанию**:
  - `RsiPeriod` = 14
  - `StdDevMultiplier` = 1m
  - `ExhaustionMultiplier` = 1m
  - `ProfitTargetTicks` = 40
  - `StopLossTicks` = 160
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Разворот
  - Направление: Оба
  - Индикаторы: RSI, StandardDeviation
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
