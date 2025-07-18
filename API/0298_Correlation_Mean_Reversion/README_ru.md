# Корреляционный средневозврат
[English](README.md) | [中文](README_zh.md)

Стратегия Correlation Mean Reversion сосредоточена на экстремальных значениях корреляции, чтобы использовать возврат к среднему. Широкие отклонения от типичного уровня редко продолжаются долго.

Сделки открываются, когда индикатор значительно отклоняется от своего среднего значения и начинает разворачиваться. Вход в длинные и короткие позиции сопровождается защитным стопом.

Подходит свинг‑трейдерам, ожидающим колебаний; стратегия закрывает позицию, когда корреляция возвращается к равновесию. Начальное значение `CorrelationPeriod` = 20.

## Детали

- **Условия входа**: Индикатор разворачивается в сторону среднего значения.
- **Длинные/Короткие**: Оба направления.
- **Условия выхода**: Индикатор возвращается к среднему.
- **Стопы**: Да.
- **Значения по умолчанию**:
  - `CorrelationPeriod` = 20
  - `LookbackPeriod` = 20
  - `DeviationThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Средневозвратная
  - Направление: Оба
  - Индикаторы: Correlation
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
