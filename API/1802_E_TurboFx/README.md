# E TurboFx Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum reversal strategy adapted from the MQL5 expert "e-TurboFx". The system watches for a series of candles whose bodies grow in size in the same direction. After several bearish candles with expanding bodies the strategy buys, expecting a rebound. After several bullish candles with growing bodies it sells. Optional stop-loss and take-profit are set in raw price points.

## Details

- **Entry Criteria**:
  - Long: `N` consecutive bearish candles and each body larger than the previous one
  - Short: `N` consecutive bullish candles and each body larger than the previous one
- **Long/Short**: Both
- **Exit Criteria**: Stop-loss or take-profit
- **Stops**: Points via `StartProtection`
- **Default Values**:
  - `BarsCount` = 3
  - `StopLossPoints` = 700
  - `TakeProfitPoints` = 1200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Price Action
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
