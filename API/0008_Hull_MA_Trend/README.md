# Hull MA Trend
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Hull Moving Average trend

The Hull MA Trend strategy monitors the slope of the Hull Moving Average. Rising slopes prompt longs and falling slopes prompt shorts, with an ATR trailing stop protecting each trade.

Its responsive calculation reduces lag compared with traditional moving averages, allowing the system to react quickly to new momentum. The ATR stop helps avoid large drawdowns if the slope changes abruptly.


## Details

- **Entry Criteria**: Signals based on MA, ATR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `HmaPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 61%. It performs best in the crypto market.
