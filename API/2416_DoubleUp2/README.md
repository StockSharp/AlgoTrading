# DoubleUp2 CCI MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

DoubleUp2 is a martingale-style strategy combining the Commodity Channel Index (CCI) and MACD.
It opens short positions when both indicators show extreme positive values and long positions when both are extremely negative.
After a losing trade the position size doubles, seeking to recover previous losses.
Profitable trades are closed once price advances by a fixed number of points.

## Details

- **Entry Criteria**:
  - **Long**: `CCI < -Threshold` and `MACD < -Threshold`.
  - **Short**: `CCI > Threshold` and `MACD > Threshold`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal or price moves `ExitDistance` points in profit.
- **Stops**: No explicit stop loss.
- **Default Values**:
  - `CCI Period` = 8
  - `MACD Fast` = 13
  - `MACD Slow` = 33
  - `MACD Signal` = 2
  - `Threshold` = 230
  - `Base Volume` = 0.1
  - `ExitDistance` = `120 * price step`
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: CCI, MACD
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
