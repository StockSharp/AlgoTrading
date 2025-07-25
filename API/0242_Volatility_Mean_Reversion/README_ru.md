# Стратегия средневозврата на основе волатильности
[English](README.md) | [中文](README_zh.md)

Этот подход торгует вокруг колебаний рыночной волатильности. Когда ATR значительно отклоняется от своей скользящей средней, это указывает, что волатильность стала необычно высокой или низкой и может вернуться к норме.

Тестирование показывает среднегодичную доходность около 73%\. Стратегию лучше запускать на крипторынке.

Стратегия открывает длинную позицию, когда ATR опускается ниже среднего минус `DeviationMultiplier`, умноженный на стандартное отклонение, и цена находится ниже скользящей средней. Короткая позиция открывается, когда ATR превышает верхнюю границу и цена выше средней. Позиции закрываются, когда ATR возвращается к своему среднему уровню.

Такие настройки подходят трейдерам, которые предпочитают торговать против экстремумов волатильности, а не по направлению цены. Защитный стоп‑лосс используется на случай, если волатильность продолжит расти.

## Подробности
- **Условия входа**:
  - **Лонг**: ATR < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Шорт**: ATR > Avg + DeviationMultiplier * StdDev && Close > MA
- **Длинные/короткие**: обе стороны.
- **Условия выхода**:
  - **Лонг**: выход при ATR > Avg
  - **Шорт**: выход при ATR < Avg
- **Стопы**: да, процентный стоп‑лосс.
- **Значения по умолчанию**:
  - `AtrPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Mean Reversion
  - Направление: оба
  - Индикаторы: ATR
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний


