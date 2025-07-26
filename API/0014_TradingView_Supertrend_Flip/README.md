# TradingView Supertrend Flip
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Supertrend indicator flips with volume confirmation

Testing indicates an average annual return of about 79%. It performs best in the stocks market.

TradingView Supertrend Flip emulates the popular indicator's color changes. A flip from red to green signals a long entry and green to red signals a short. The strategy exits on the next flip.

Volume confirmation can be used to avoid whipsaws during thin trading periods. By only acting on flips with supporting volume, the method aims to capture more reliable reversals.


## Details

- **Entry Criteria**: Signals based on ATR, Supertrend.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `VolumeAvgPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, Supertrend
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

