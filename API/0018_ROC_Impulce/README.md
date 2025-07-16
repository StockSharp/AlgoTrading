# ROC Impulce
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Rate of Change (ROC) impulse

ROC Impulse captures sudden bursts in the Rate of Change indicator. Sharp positive spikes lead to long trades and sharp negatives to short trades. When momentum fades back toward zero the position is closed.

The trigger levels can be tuned to react only to exceptional momentum events. ATR-based stops help prevent large losses if the spike quickly reverses.


## Details

- **Entry Criteria**: Signals based on ATR, ROC, Momentum.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `RocPeriod` = 12
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, ROC, Momentum
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
