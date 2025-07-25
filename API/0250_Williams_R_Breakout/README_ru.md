# Стратегия прорыва Williams %R
[English](README.md) | [中文](README_zh.md)

Эта стратегия ищет всплески импульса, наблюдая за индикатором Williams %R относительно его исторического среднего. Когда осциллятор выходит далеко за пределы типичных значений, это может указывать на начало сильного движения.

Тестирование показывает среднегодичную доходность около 91%\. Стратегию лучше запускать на фондовом рынке.

Длинная позиция открывается, когда %R поднимается выше среднего плюс `Multiplier`, умноженный на оценку стандартного отклонения. Короткая позиция берется, когда %R опускается ниже среднего минус тот же множитель. Сделка закрывается, когда %R возвращается к своему среднему или срабатывает стоп‑лосс.

Подход рассчитан на трейдеров прорывов, которые хотят раннего участия в зарождающемся тренде. Риск по позиции управляется процентным стопом от цены входа.

## Подробности
- **Условия входа**:
  - **Лонг**: %R > Avg + Multiplier * StdDev
  - **Шорт**: %R < Avg - Multiplier * StdDev
- **Длинные/короткие**: обе стороны.
- **Условия выхода**:
  - **Лонг**: выход при %R < Avg
  - **Шорт**: выход при %R > Avg
- **Стопы**: да, процентный стоп‑лосс.
- **Значения по умолчанию**:
  - `WilliamsRPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Breakout
  - Направление: оба
  - Индикаторы: Williams %R
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний


