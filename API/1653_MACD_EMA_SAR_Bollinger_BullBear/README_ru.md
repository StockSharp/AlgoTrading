# Стратегия MACD EMA SAR Bollinger BullBear
[English](README.md) | [中文](README_cn.md)

Объединяет индикаторы MACD, пересечение EMA, Parabolic SAR, полосы Боллинджера и Bulls/Bears Power. Торгует только в активные часы.

## Детали

- **Условия входа**:
  - **Long**: MACD < Signal, два предыдущих максимума ниже верхней полосы Боллинджера, EMA3 > EMA34, SAR ниже цены, Bulls Power > 0 и снижается.
  - **Short**: MACD > Signal, EMA3 < EMA34, SAR выше цены, Bears Power < 0 и растёт.
- **Long/Short**: Оба направления.
- **Условия выхода**:
  - Нет специальных правил выхода; позиция закрывается по противоположному сигналу.
- **Стопы**: Отсутствуют.
- **Значения по умолчанию**:
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Fast EMA Period` = 3
  - `Slow EMA Period` = 34
  - `Power Period` = 13
  - `SAR Step` = 0.02
  - `SAR Max` = 0.2
  - `Bollinger Period` = 20
  - `Bollinger Deviation` = 2.0
  - `Candle Type` = 15 минут
  - `Session Start` = 09:00
  - `Session End` = 17:00
- **Фильтры**:
  - Категория: Trend Following
  - Направление: Оба
  - Индикаторы: Несколько
  - Стопы: Нет
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
