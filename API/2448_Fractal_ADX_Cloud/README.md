# Fractal ADX Cloud
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy approximates the original MQL `Fractal_ADX_Cloud` expert by using the Average Directional Index indicator in StockSharp. It works on four‑hour candles and analyses the cross of the +DI and -DI components. When the bullish component (+DI) rises above the bearish one (-DI), the strategy closes any short position and may open a new long. If -DI rises above +DI the logic is mirrored for short trades.

Stop-loss and take-profit protections are applied in absolute price units. Additional parameters allow enabling or disabling opening and closing of positions in each direction.

## Details

- **Entry Criteria**: Cross of +DI and -DI lines from ADX.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes, using absolute price distances.
- **Default Values**:
  - `AdxPeriod` = 30
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ADX
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
