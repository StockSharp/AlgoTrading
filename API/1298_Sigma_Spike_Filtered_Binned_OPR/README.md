# Sigma Spike Filtered Binned OPR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Sigma Spike Filtered Binned OPR collects the open-position ratio (OPR) distribution and trades when OPR reaches extreme bins after a sigma spike in returns.

## Details

- **Entry Criteria**: OPR at extreme bins (<= `OprThreshold` or >= `100 - OprThreshold`) with optional sigma spike filter
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `SigmaSpikeLength` = 20
  - `FilterBySigmaSpike` = true
  - `SigmaSpikeThreshold` = 2
  - `OprThreshold` = 10
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
