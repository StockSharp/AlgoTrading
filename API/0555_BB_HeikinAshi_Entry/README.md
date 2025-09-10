# BB HeikinAshi Entry
[Русский](README_ru.md) | [中文](README_cn.md)

Bollinger Bands strategy using Heikin Ashi candles.

The system waits for two or three consecutive bearish Heikin Ashi bars that touch the lower Bollinger Band. A bullish candle closing back above the band triggers a long entry. Shorts work in the opposite direction. Half of the position is closed at the first target and the remainder is protected with a trailing stop.

## Details

- **Entry Criteria**: Reversal of consecutive Heikin Ashi candles around Bollinger Bands.
- **Long/Short**: Both.
- **Exit Criteria**: Partial take profit and trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `BollingerLength` = 20
  - `BollingerWidth` = 2
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Heikin Ashi, Bollinger Bands
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

