# Модель осциллятора динамических тиков (DTOM)
[English](README.md) | [中文](README_cn.md)

**Dynamic Ticks Oscillator Model** использует скорость изменения индекса NYSE Down Ticks. Когда ROC опускается ниже динамического порога, рассчитанного через стандартное отклонение, стратегия открывает длинную позицию. Позиция закрывается, когда ROC поднимается выше положительного порога.

## Подробности
- **Критерии входа**: `ROC < -StdDev * EntryStdDevMultiplier`
- **Длинные/короткие**: только длинные.
- **Критерии выхода**: `ROC > StdDev * ExitStdDevMultiplier`
- **Стопы**: нет.
- **Значения по умолчанию**:
  - `RocLength = 5`
  - `VolatilityLookback = 24`
  - `EntryStdDevMultiplier = 1.6m`
  - `ExitStdDevMultiplier = 1.4m`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Фильтры**:
  - Категория: Средний возврат
  - Направление: Длинные
  - Индикаторы: RateOfChange, StandardDeviation
  - Стопы: Нет
  - Сложность: Начальная
  - Таймфрейм: Дневной
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

