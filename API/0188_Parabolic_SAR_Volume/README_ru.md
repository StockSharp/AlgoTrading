# Parabolic Sar Volume Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия, которая сочетает Parabolic SAR с подтверждением объёма. Вход в сделки происходит, когда цена пересекает Parabolic SAR при объёме выше среднего.

Тестирование показывает среднегодичную доходность около 151%\. Стратегию лучше запускать на фондовом рынке.

Parabolic SAR определяет разворот тренда, а повышенный объём подтверждает сигнал. Сделки открываются, когда смена SAR сопровождается ростом объёма.

Полезно трейдерам, отслеживающим движения на объёме. След SAR и коэффициент ATR защищают от больших убытков.

## Детали

- **Критерии входа**:
  - Long: `Close > SAR && Volume > AvgVolume`
  - Short: `Close < SAR && Volume > AvgVolume`
- **Long/Short**: Оба направления
- **Критерии выхода**: разворот SAR
- **Стопы**: использование Parabolic SAR как трейлинг-стоп
- **Значения по умолчанию**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `VolumePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Breakout
  - Направление: Оба
  - Индикаторы: Parabolic SAR, Parabolic SAR, Volume
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

