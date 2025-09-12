# RSI & Backed-Weighted MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses the Relative Strength Index and a retro weighted moving average with a rate-of-change filter. Long positions open when RSI exceeds the threshold and MA ROC is below the set level, while short positions open on the opposite conditions. The system applies an ATR-based trailing stop and fixed ratio position sizing.

## Details

- **Entry Criteria**:
  - **Long**: `RSI >= RsiLongSignal` and `MA ROC <= RocMaLongSignal`
  - **Short**: `RSI <= RsiShortSignal` and `MA ROC >= RocMaShortSignal`
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal, stop loss or trailing stop.
- **Stops**: Yes, ATR trailing stop and max loss percent.
- **Default Values**:
  - `RsiLength` = 20
  - `MaType` = RWMA
  - `MaLength` = 19
  - `RsiLongSignal` = 60
  - `RsiShortSignal` = 40
  - `TakeProfitActivation` = 5
  - `TrailingPercent` = 3
  - `MaxLossPercent` = 10
  - `FixedRatio` = 400
  - `IncreasingOrderAmount` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: RSI, Moving Average, ATR
  - Stops: Yes
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
