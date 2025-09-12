# Hull Candles
[Русский](README_ru.md) | [中文](README_cn.md)

Hull Candles is a simple trend-following strategy using a Hull Moving Average of the average price (OHLC4). When the HMA rises and the close is above its SMA, it enters long positions; when the HMA falls and the close is below its SMA, it enters short positions.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: HMA rises and close > SMA.
  - **Short**: HMA falls and close < SMA.
- **Exit Criteria**: Reverse signal.
- **Stops**: None.
- **Default Values**:
  - `BodyLength` = 10
  - `SmaLength` = 1
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: HMA, SMA
  - Complexity: Low
  - Risk level: High
