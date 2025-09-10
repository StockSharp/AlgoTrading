# CE ZLSMA 5MIN Candlechart Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following system using Zero Lag LSMA on Heikin Ashi candles with a Chandelier Exit filter. Buys when trend flips bullish and the candle closes above the ZLSMA.

## Details

- **Entry Criteria**:
  - Long: direction turns up, Heikin Ashi close above ZLSMA and open
- **Long/Short**: Long
- **Exit Criteria**:
  - Long: close below ZLSMA
- **Stops**: None
- **Default Values**:
  - `ZlsmaLength` = 50
  - `AtrPeriod` = 1
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend Following
  - Direction: Long
  - Indicators: ZLSMA, ATR, Heikin Ashi
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
