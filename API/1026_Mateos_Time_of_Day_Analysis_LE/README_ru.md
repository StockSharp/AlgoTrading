# Mateo's Time of Day Analysis LE
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Открывает длинную позицию в заданном внутридневном окне и закрывает её позже в течение дня.

Эта стратегия помогает исследовать влияние времени суток.

## Детали

- **Условия входа**: Время достигает `StartTime` в пределах диапазона `From`–`Thru`.
- **Long/Short**: Только long.
- **Условия выхода**: Время достигает `EndTime` (до 20:00).
- **Стопы**: Нет.
- **Значения по умолчанию**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `StartTime` = 09:30
  - `EndTime` = 16:00
  - `From` = 2017-04-21
  - `Thru` = 2099-12-01
- **Фильтры**:
  - Category: Time-based
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
