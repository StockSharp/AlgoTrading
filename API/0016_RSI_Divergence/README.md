# RSI Divergence
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on RSI divergence

Testing indicates an average annual return of about 85%. It performs best in the crypto market.

RSI Divergence searches for price extremes unconfirmed by the RSI oscillator. A bullish divergence leads to a buy and a bearish divergence prompts a sell. The trade lasts until RSI reverses or a stop fires.

Divergence setups often emerge near the end of long trends. By comparing the oscillator's behavior with price action, the strategy attempts to catch early reversals with controlled risk.


## Details

- **Entry Criteria**: Signals based on RSI.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium

