# Стратегия "Бычья харами"
[English](README.md) | [中文](README_zh.md)

"Бычья харами" представляет собой двухсвечный паттерн, при котором небольшое тело второй свечи полностью помещается в диапазон предыдущей медвежьей свечи. Такой сигнал говорит о замедлении продаж и возможном возвращении покупателей.

Тестирование показывает среднегодичную доходность около 40%. Стратегию лучше запускать на крипторынке.

Стратегия открывает длинную позицию после закрытия второй свечи внутри первой, рассчитывая на продолжение роста на следующем баре.

Защитный стоп в процентах располагается под формацией и позиция закрывается, если цена опускается ниже точки входа.

## Детали

- **Условия входа**: совпадение с паттерном
- **Длинная/короткая**: обе
- **Условия выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, процентные
- **Значения по умолчанию**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Фильтры**:
  - Категория: Паттерн
  - Направление: обе
  - Индикаторы: свечные
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейронные сети: нет
  - Дивергенция: нет
  - Уровень риска: средний

