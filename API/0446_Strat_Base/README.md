# Strategy Base Template
[Русский](README_ru.md) | [中文](README_cn.md)

This folder provides a minimal scaffold for building custom trading ideas. The
strategy only calculates a single exponential moving average and exposes a wide
range of common parameters: enabling long or short trades, optional take profit
and stop loss, and optimization ranges. Developers can insert their own entry
and exit logic inside the placeholders to rapidly prototype new systems.

The template also demonstrates how to start the built‑in protection module with
percentage‑based targets, making it easy to experiment with different risk
settings. Because no real signals are included, this script is not meant to be
traded as‑is but rather to serve as a starting point for further research.

## Details

- **Entry Criteria**: Not implemented – replace with custom rules.
- **Long/Short**: Configurable via parameters.
- **Exit Criteria**: Not implemented – replace with custom rules.
- **Stops**: Optional percent take profit and stop loss handled by protection module.
- **Default Values**:
  - EMA length = 10.
  - Take profit = 1.2%, Stop loss = 1.8% (disabled by default).
- **Filters**:
  - Category: Template
  - Direction: Configurable
  - Indicators: EMA
  - Stops: Optional
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: User defined
