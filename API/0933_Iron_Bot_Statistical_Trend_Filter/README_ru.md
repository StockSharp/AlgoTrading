# Стратегия Iron Bot Statistical Trend Filter
[English](README.md) | [中文](README_cn.md)

Эта стратегия торгует пробои уровней, рассчитанных на основе уровней Фибоначчи и Z-Score.

## Детали

- **Условия входа**:
  - **Long**: цена пересекает трендовую линию и верхний уровень при неотрицательном Z-score.
  - **Short**: цена опускается ниже трендовой линии и нижнего уровня при неположительном Z-score.
- **Long/Short**: обе стороны.
- **Условия выхода**:
  - Стоп-лосс на расстоянии `SlRatio` процентов от входа.
  - Тейк-профит на одном из четырех уровней (`Tp1Ratio`–`Tp4Ratio`) от входа.
- **Stops**: Да.
- **Значения по умолчанию**:
  - `ZLength` = 40.
  - `AnalysisWindow` = 44.
  - `HighTrendLimit` = 0.236.
  - `LowTrendLimit` = 0.786.
  - `EmaLength` = 200.
- **Фильтры**:
  - Категория: Trend following
  - Direction: Both
  - Indicators: Z-score, EMA, price action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
