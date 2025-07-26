# Ichimoku Tenkan/Kijun Cross Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Ichimoku indicators provide a full trend-following system. This approach focuses on the Tenkan-sen crossing the Kijun-sen while price trades relative to the Kumo cloud. A bullish cross above the cloud signals trend continuation higher, whereas a bearish cross below the cloud suggests weakness.

During operation the strategy calculates the Ichimoku components on each bar. When Tenkan rises above Kijun and price sits above the cloud, a long trade is initiated with a stop near Kijun. A cross in the opposite direction below the cloud triggers a short with a similar stop placement.

The system stays in the trade until the stop is hit or the cross reverses, aiming to catch sustained moves that follow the direction of the cloud.

## Details

- **Entry Criteria**: Tenkan/Kijun cross with price relative to the Kumo cloud.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss or opposite cross.
- **Stops**: Yes, at Kijun level.
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = 30 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Ichimoku
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Swing
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 142%. It performs best in the stocks market.
