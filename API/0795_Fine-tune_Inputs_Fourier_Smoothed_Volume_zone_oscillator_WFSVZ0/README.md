# Fourier Smoothed Volume Zone Oscillator WFSVZ0
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using a Fourier-smoothed Volume Zone Oscillator. It goes long when the oscillator rises above a threshold and short when it falls below the negative threshold. Optionally closes open positions when no signal is present.

## Details

- **Entry Criteria**: Oscillator rising above threshold / falling below negative threshold.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or optional close-all.
- **Stops**: None.
- **Default Values**:
  - `VzoLength` = 2
  - `SmoothLength` = 2
  - `Threshold` = 0m
  - `CloseAllPositions` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: Volume Zone Oscillator
  - Stops: None
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
