# Стратегия Berlin Candles
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия использует собственные свечи Berlin, построенные на сглаженных значениях Heikin Ashi. Длинная позиция открывается, когда бычья свеча Berlin закрывается выше линии Дончиана. Короткая позиция открывается, когда медвежья свеча закрывается ниже этой линии.

## Детали

- **Условия входа**:
  - **Лонг**: закрытие Berlin > открытие Berlin и закрытие Berlin > baseline.
  - **Шорт**: закрытие Berlin < открытие Berlin и закрытие Berlin < baseline.
- **Направление**: обе стороны
- **Стопы**: по умолчанию отсутствуют
- **Значения по умолчанию**:
  - `Smoothing` = 1
  - `BaselinePeriod` = 26
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
