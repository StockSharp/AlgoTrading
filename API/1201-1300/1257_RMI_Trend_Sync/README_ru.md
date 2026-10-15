# Стратегия RMI Trend Sync
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия объединяет импульс по RSI и MFI с трейлинг-стопом SuperTrend. Лонг открывается при превышении средним импульсом верхнего порога и растущем наклоне EMA, шорт — при пробое вниз. Выход осуществляется по противоположному импульсу или по линии SuperTrend.

## Подробности

- **Условия входа**: Средний импульс пересекает пороги с подтверждением наклона EMA.
- **Длинные/короткие позиции**: обе стороны.
- **Условия выхода**: противоположный импульс или стоп SuperTrend.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `RmiLength` = 21
  - `PositiveThreshold` = 70
  - `NegativeThreshold` = 30
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3.5
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Trend
  - Направление: обе стороны
  - Индикаторы: RSI, MFI, EMA, SuperTrend
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний
