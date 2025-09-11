[English](README.md) | [中文](README_cn.md)

SMC стратегия определяет зоны premium, equilibrium и discount на основе недавних swing high и swing low. Торгует в зонах discount или premium с фильтром тенденции SMA и простым подтверждением order block.

## Детали

- **Условия входа**: цена в зоне discount выше SMA и поддержкой order block; цена в зоне premium ниже SMA и с сопротивлением order block
- **Длинные/Короткие**: Оба
- **Условия выхода**: противоположный сигнал
- **Стопы**: Нет
- **Значения по умолчанию**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
- **Фильтры**:
  - Категория: Zone
  - Направление: Оба
  - Индикаторы: Highest, Lowest, SMA
  - Стопы: Нет
  - Сложность: Basic
  - Таймфрейм: Intraday
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Medium
