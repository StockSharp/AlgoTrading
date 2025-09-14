# Universal Trailing Stop Hedge Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy demonstrating different trailing stop techniques to protect open positions.
It offers ATR-, Parabolic SAR-, moving average-, percentage- and fixed-pip based trailing stops.
A simple entry based on candle direction is used purely for educational purposes.

## Details

- **Entry Criteria**: Long if candle closes above open, short if closes below open
- **Long/Short**: Both
- **Exit Criteria**: Trailing stop hit
- **Stops**: ATR, Parabolic SAR, Moving Average, Percent Profit or fixed pips depending on selected mode
- **Default Values**:
  - `Mode` = `TrailingMode.Atr`
  - `Delta` = 10
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `MaPeriod` = 34
  - `PercentProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: ATR, Parabolic SAR, SMA
  - Stops: Trailing stop
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
