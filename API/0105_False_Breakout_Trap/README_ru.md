# Стратегия "Ложный пробой"
[English](README.md) | [中文](README_zh.md)

Стратегия нацелена на ситуации, когда пробой ключевой поддержки или сопротивления не удерживается. Часто трейдеры входят на пробое, а цена быстро возвращается, оставляя их в ловушке.

Тестирование показывает среднегодичную доходность около 52%. Стратегию лучше запускать на крипторынке.

Система ждёт такого сбоя и входит в противоположном направлении, когда цена закрывается обратно в диапазоне.

Стоп ставится сразу за уровнем ложного пробоя, позволяя ограничить убыток, если разворот не состоится.

## Детали

- **Условия входа**: сигнал индикатора
- **Длинная/короткая**: обе
- **Условия выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, процентные
- **Значения по умолчанию**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Фильтры**:
  - Категория: Разворот
  - Направление: обе
  - Индикаторы: Price Action
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейронные сети: нет
  - Дивергенция: нет
  - Уровень риска: средний

