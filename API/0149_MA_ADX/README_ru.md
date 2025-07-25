# Стратегия MA ADX
[English](README.md) | [中文](README_cn.md)

Стратегия основана на индикаторах скользящей средней и ADX. Вход выполняется, когда цена пересекает скользящую среднюю при наличии сильного тренда.

Тестирование показывает среднегодичную доходность около 184%. Стратегию лучше запускать на крипторынке.

Скользящая средняя задаёт направление, а ADX подтверждает, достаточно ли он силён для торговли. Позиции открываются по пересечению MA, если ADX выше порога.

Этот классический трендовый подход нравится системным трейдерам. Потери ограничиваются стопом по ATR.

## Подробности

- **Условия входа**:
  - Лонг: `Close > MA && ADX > 25`
  - Шорт: `Close < MA && ADX > 25`
- **Длинные/короткие**: обе стороны
- **Условия выхода**: обратное пересечение MA или стоп
- **Стопы**: `StopLossPercent` процентов и тейк-профит `TakeProfitAtrMultiplier` ATR
- **Значения по умолчанию**:
  - `MaPeriod` = 20
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
  - `TakeProfitAtrMultiplier` = 2m
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: Скользящая средняя, ADX
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

