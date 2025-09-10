# CVD Divergence Volume HMA RSI MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines Hull Moving Averages, RSI, MACD, volume filter, and cumulative volume delta (CVD) divergence to identify trend opportunities.

Long positions open when HMA20 is above HMA50, RSI shows bullish momentum, MACD histogram rises, volume exceeds its average, and CVD forms bullish divergence or increases. Short positions mirror these conditions.

## Details
- **Entry Criteria**:
  - **Long**: HMA20 > HMA50 & price > HMA20; RSI between 40 and `RsiOverbought`; MACD line above signal & histogram rising; volume > SMA * `VolumeMultiplier`; bullish CVD divergence or increasing CVD.
  - **Short**: HMA20 < HMA50 & price < HMA20; RSI between `RsiOversold` and 60; MACD line below signal & histogram falling; volume > SMA * `VolumeMultiplier`; bearish CVD divergence or decreasing CVD.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Price < HMA20 or RSI > `RsiOverbought` or MACD line crosses below signal.
  - **Short**: Price > HMA20 or RSI < `RsiOversold` or MACD line crosses above signal.
- **Stops**: No.
- **Default Values**:
  - `Hma20Length` = 20
  - `Hma50Length` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: HMA, RSI, MACD, Volume, CVD
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium
