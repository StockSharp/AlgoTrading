# ColorJFatl Digit ReOpen Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses a Jurik Moving Average (JMA) to identify trend direction. A long position opens when the JMA turns upward and all short positions are closed. A short position opens when the JMA turns downward and all long positions are closed. Additional positions are added every time price moves a fixed number of points in the trade direction, up to a maximum count.

## Details

- **Entry**:
  - JMA changes direction upward → open long and close shorts.
  - JMA changes direction downward → open short and close longs.
- **Re-entry**:
  - After the initial position, new positions open every `PriceStep` points in the trade direction until `MaxPositions` is reached.
- **Exit**:
  - Opposite JMA turn closes current positions.
- **Parameters**:
  - `JmaLength` – JMA period.
  - `PriceStep` – price movement in points required for re-entry.
  - `MaxPositions` – maximum number of concurrent positions.
  - `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – enable or disable actions.
  - `CandleType` – timeframe for calculations.
- **Indicator**: Jurik Moving Average.
- **Type**: Trend following.
- **Timeframe**: 4 hours by default.
