# IMA Expert Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading on the relative speed of price against its moving average.
The ratio `Close / SMA - 1` is compared between two consecutive candles. A strong increase opens a long position, while a strong decrease opens a short position.

## Details

- **Entry Criteria**:
  - Long: `(IMA_now - IMA_prev) / abs(IMA_prev) >= SignalLevel`
  - Short: `(IMA_now - IMA_prev) / abs(IMA_prev) <= -SignalLevel`
- **Exit Criteria**: Opposite signal
- **Position Sizing**: `RiskLevel` and `StopLossTicks` define trade volume, limited by `MaxVolume`
- **Long/Short**: Both
- **Stops**: None
- **Default Values**:
  - `SmaPeriod` = 5
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 1000
  - `SignalLevel` = 0.5
  - `RiskLevel` = 0.01
  - `MaxVolume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
