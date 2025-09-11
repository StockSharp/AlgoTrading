# Стратегия TrendGuard Flag Finder
[English](README.md) | [中文](README_cn.md)

TrendGuard Flag Finder ищет бычьи и медвежьи флаги с подтверждением SuperTrend.
Покупает при пробое бычьего флага и продаёт при пробое медвежьего флага.

## Детали

- **Критерий входа**: пробой флага с подтверждением SuperTrend
- **Длин/Шорт**: настраивается
- **Критерий выхода**: противоположный пробой флага
- **Стопы**: нет
- **Значения по умолчанию**:
  - `TradingDirection` = Both
  - `SuperTrend Length` = 10
  - `SuperTrend Factor` = 4
  - `MaxFlagDepth` = 5
  - `MinFlagLength` = 3
  - `MaxFlagLength` = 7
  - `MaxFlagRally` = 5
  - `MinBearFlagLength` = 3
  - `MaxBearFlagLength` = 7
  - `PoleMin` = 3
  - `PoleLength` = 7
  - `PoleMinBear` = 3
  - `PoleLengthBear` = 7
- **Фильтры**:
  - Категория: Паттерн
  - Направление: Настраиваемое
  - Индикаторы: SuperTrend, Lowest, Highest
  - Стопы: Нет
  - Сложность: Продвинутая
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
