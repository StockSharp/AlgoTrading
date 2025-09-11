# EMA Dow Theory Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a fast and slow Exponential Moving Average (EMA) crossover with a basic Dow Theory trend filter. The trend is determined by recent swing highs and lows. Positions are taken when the EMAs align with the trend direction.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA ≥ Slow EMA and price breaks above the last swing high.
  - **Short**: Fast EMA < Slow EMA and price breaks below the last swing low.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - Fast EMA length = 47
  - Slow EMA length = 50
  - Swing length = 6 bars
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, swing high/low
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
