# Channel Scalper Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

An ATR based channel breakout scalping system. For each candle the midpoint is calculated as the average of high and low. Upper and lower bands are built by adding and subtracting the Average True Range multiplied by a factor. When the close breaks above the previous upper band a long position is opened. A break below the lower band triggers a short position. Bands trail in the trade direction and serve as dynamic stops; a cross of the opposite band reverses the position.

## Details

- **Entry Criteria**:
  - **Buy**: Closing price crosses above the prior upper band.
  - **Sell**: Closing price crosses below the prior lower band.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Reverse signal when price crosses the opposite band.
- **Stops**: Trailing channel bands act as stops.
- **Filters**: None.

## Parameters

- **ATR Period** – number of bars used for ATR calculation.
- **ATR Multiplier** – factor applied to ATR for band distance.
- **Candle Type** – timeframe of input candles.
