# Nadaraya-Watson Envelope Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Builds Nadaraya-Watson kernel regression envelopes in log scale. Goes long when price crosses above the lower envelope and optionally goes short when price crosses below the upper envelope.

## Details

- **Entry Criteria**:
  - Long when close crosses above the lower envelope.
  - Short when close crosses below the upper envelope (in Long/Short mode).
- **Long/Short**: Configurable.
- **Exit Criteria**: Opposite envelope cross.
- **Stops**: No.
- **Default Values**:
  - `LookbackWindow` = 8
  - `RelativeWeighting` = 8
  - `StartRegressionBar` = 25
  - `StrategyType` = Long Only
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Envelope
  - Direction: Configurable
  - Indicators: Nadaraya-Watson
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
