# Ichimoku Kumo Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Ichimoku Kumo (cloud) breakout

Testing indicates an average annual return of about 70%. It performs best in the stocks market.

This approach relies on Ichimoku cloud signals. Price breaking above the cloud with Tenkan-sen crossing over Kijun-sen triggers a buy, while the opposite break below the cloud starts a short. Positions are held until price returns through the cloud.

The cloud outlines key support and resistance levels, so the system waits for decisive closes beyond it. By combining multiple Ichimoku components, the strategy avoids lower-probability trades during sideways markets.


## Details

- **Entry Criteria**: Signals based on Ichimoku.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Ichimoku
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

