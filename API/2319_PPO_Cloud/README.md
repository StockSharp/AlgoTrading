# PPO Cloud Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This momentum strategy trades crossovers between the Percentage Price Oscillator (PPO) and its signal line. A long position opens when the PPO crosses above its signal line, while a short position opens on the opposite crossover. Existing positions can optionally be closed on the contrary signal. The strategy operates on a single timeframe.

## Details

- **Entry Criteria**:
  - **Long**: `PPO crosses above signal`.
  - **Short**: `PPO crosses below signal`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Long**: `PPO crosses below signal` (optional).
  - **Short**: `PPO crosses above signal` (optional).
- **Stops**: None.
- **Default Values**:
  - `Fast Period` = 12.
  - `Slow Period` = 26.
  - `Signal Period` = 9.
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
