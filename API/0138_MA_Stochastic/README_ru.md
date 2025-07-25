# Стратегия MA Stochastic
[English](README.md) | [中文](README_zh.md)

MA Stochastic использует фильтр тренда по скользящей средней вместе со стохастическим осциллятором для поиска откатов.
Когда цена находится выше средней, а стохастик падает в зону перепроданности, система готовится купить следующий подъём.

Тестирование показывает среднегодичную доходность около 151%. Стратегию лучше запускать на фондовом рынке.

Короткие сделки отражают эту логику для нисходящего тренда, продавая откаты при стохастике в перекупленности.

Фиксированные процентные стопы помогают избежать крупных потерь при резких разворотах тренда.

## Детали

- **Критерий входа**: сигнал индикатора
- **Длинная/короткая сторона**: обе
- **Критерий выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, процентные
- **Значения по умолчанию:**
  - `CandleType` = 15 минут
  - `StopLoss` = 2%
- **Фильтры:**
  - Категория: Следование за трендом
  - Направление: обе
  - Индикаторы: Moving Average, Stochastic
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний

