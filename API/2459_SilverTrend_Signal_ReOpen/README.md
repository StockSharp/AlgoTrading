# SilverTrend Signal ReOpen Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the SilverTrend indicator with optional re-entry. Opens a position when the indicator changes direction and adds additional positions every time the price moves a defined step in favor of the trade. Positions can be closed on opposite signals or when stop loss / take profit levels are hit.

## Details

- **Entry Criteria**:
  - Long: SilverTrend indicator switches from downtrend to uptrend
  - Short: SilverTrend indicator switches from uptrend to downtrend
- **Long/Short**: Both
- **Exit Criteria**:
  - Optionally close on opposite SilverTrend signals
  - Stop Loss or Take Profit reached
- **Stops**: Absolute price levels
- **Default Values**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Ssp` = 9
  - `Risk` = 3
  - `PriceStep` = 300m
  - `PosTotal` = 10
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SilverTrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
