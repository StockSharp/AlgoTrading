# EMA: средневозврат по наклону
[English](README.md) | [中文](README_zh.md)

Стратегия EMA Slope Mean Reversion сосредоточена на экстремальных значениях экспоненциальной скользящей средней, чтобы использовать возврат к среднему. Широкие отклонения от недавнего уровня редко продолжаются долго.

Сделки открываются, когда индикатор значительно отклоняется от своего среднего значения и начинает разворачиваться. Вход в длинные и короткие позиции сопровождается защитным стопом.

Подходит свинг‑трейдерам, ожидающим колебаний; стратегия закрывает позицию, когда EMA возвращается к равновесию. Начальное значение `EmaPeriod` = 20.

## Детали

- **Условия входа**: Индикатор разворачивается в сторону среднего значения.
- **Длинные/Короткие**: Оба направления.
- **Условия выхода**: Индикатор возвращается к среднему.
- **Стопы**: Да.
- **Значения по умолчанию**:
  - `EmaPeriod` = 20
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Средневозвратная
  - Направление: Оба
  - Индикаторы: EMA
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
