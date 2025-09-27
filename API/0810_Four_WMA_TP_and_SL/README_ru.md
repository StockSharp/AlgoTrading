# Стратегия четырёх WMA с TP и SL
[English](README.md) | [中文](README_cn.md)

Стратегия, использующая пересечение четырёх скользящих средних с опциональными тейк-профитом, стоп-лоссом и альтернативным выходом.

## Детали

- **Условия входа**:
  - Long: Long MA1 пересекает Long MA2 снизу вверх
  - Short: Short MA1 пересекает Short MA2 сверху вниз
- **Long/Short**: Настраиваемо
- **Стопы**: Процентные TP и SL
- **Значения по умолчанию**:
  - `LongMa1Length` = 10
  - `LongMa2Length` = 20
  - `ShortMa1Length` = 30
  - `ShortMa2Length` = 40
  - `MaType` = Wma
  - `EnableTpSl` = true
  - `TakeProfitPercent` = 1m
  - `StopLossPercent` = 1m
  - `Direction` = Both
  - `EnableAltExit` = false
  - `AltExitMaOption` = LongMa1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: Скользящие средние
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
