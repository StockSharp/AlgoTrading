# Traffic Light Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A trend-following approach that uses a set of moving averages colored like a traffic light to determine the trading direction.
The strategy waits for price to be inside a predefined zone and then checks the order of the averages before entering the market.

## Details

- **Entry Zone**:
  - Default: price must lie between the red (slow) and yellow (medium) SMAs.
  - If `UseBlueRange` is enabled: price must lie between the high and low lines of the blue EMA channel.
- **Entry Criteria**:
  - Long: `green EMA > blueHigh EMA > yellow SMA > red SMA` and `price > green EMA`.
  - Short: `green EMA < blueLow EMA < yellow SMA < red SMA` and `price < green EMA`.
- **Exit Criteria**:
  - Optional: if `CloseOnCross` is enabled the position closes when the green EMA crosses the yellow SMA in the opposite direction.
- **Stops**: Optional take profit and stop loss measured in price steps.
- **Long/Short**: Both directions.
- **Default Values**:
  - `RedMaPeriod` = 120
  - `YellowMaPeriod` = 55
  - `GreenMaPeriod` = 5
  - `BlueMaPeriod` = 24
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TakeProfitTicks` = 120
  - `StopLossTicks` = 60
  - `UseBlueRange` = false
  - `CloseOnCross` = true
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving averages
  - Stops: Optional
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Moderate
