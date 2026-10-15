# Стратегия Price Radio
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия реализует индикатор Price Radio Джона Эллерса. Покупка происходит, когда производная цены превышает амплитуду и частоту, продажа — при обратном условии.

## Детали

- **Условия входа**:
  - **Лонг**: производная больше амплитуды и частоты.
  - **Шорт**: производная меньше отрицательных значений амплитуды и частоты.
- **Длинные/короткие**: обе стороны.
- **Условия выхода**: противоположный сигнал.
- **Стопы**: нет.
- **Значения по умолчанию**:
  - `Length` = 14.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Фильтры**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Custom
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
