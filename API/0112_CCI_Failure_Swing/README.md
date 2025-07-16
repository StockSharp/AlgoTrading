# CCI Failure Swing Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The CCI Failure Swing is based on the Commodity Channel Index forming a lower high above +100 or a higher low below -100.
This inability to make a new extreme often signals the end of the prior trend.

The strategy goes long when CCI holds above -100 and turns up, or short when it fails near +100 and turns down.

A percent stop keeps risk small and trades exit if the CCI crosses back through the previous swing level.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
