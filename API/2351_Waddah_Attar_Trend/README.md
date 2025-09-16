# Waddah Attar Trend
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy converts the original MQL expert "Exp_Waddah_Attar_Trend" into the StockSharp high-level API. It uses the Waddah Attar Trend indicator, which multiplies the difference between two exponential moving averages (fast and slow) by an additional smoothing moving average. The indicator outputs a color state: green when the trend value rises and magenta when it falls. A change of this color triggers trades.

Long positions are opened when the color switches from down to up. Short positions are opened when it switches from up to down. The strategy works on both sides and supports protective stop-loss and take-profit expressed as percentages of the entry price.

## Details

- **Entry Criteria**: Color change of Waddah Attar Trend (MACD difference multiplied by MA).
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite color change or protective stops.
- **Stops**: Yes.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `MaLength` = 9
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `StopLossPercent` = 1.0
  - `TakeProfitPercent` = 2.0
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: H4
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
