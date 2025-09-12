# PercentX Trend Follower
[English](README.md) | [中文](README_cn.md)

Стратегия, основанная на PercentX Trend Follower от Trendoscope.

Стратегия нормализует расстояние цены от выбранного канала (Keltner или Bollinger) и торгует при пересечении осциллятором динамических экстремальных диапазонов. Для установки стопов используется ATR.

## Детали

- **Условия входа**: Осциллятор пересекает верхний диапазон для лонга, нижний диапазон для шорта.
- **Длинные/Короткие**: Оба направления.
- **Условия выхода**: Стоп по ATR.
- **Стопы**: Начальный стоп по ATR.
- **Значения по умолчанию**:
  - `BandType` = Keltner
  - `MaLength` = 40
  - `LoopbackPeriod` = 80
  - `OuterLoopback` = 80
  - `UseInitialStop` = true
  - `AtrLength` = 14
  - `TrendMultiplier` = 1
  - `ReverseMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Trend
  - Направление: Both
  - Индикаторы: BollingerBands, KeltnerChannels, ATR, Highest, Lowest
  - Стопы: ATR
  - Сложность: Средняя
  - Таймфрейм: Интрадей (5м)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

