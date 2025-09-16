# Color Zerolag X10 MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a simplified port of the MetaTrader example **Exp_ColorZerolagX10MA.mq5**. It uses a zero lag exponential moving average to detect slope changes. When the moving average turns upward after decreasing for two bars, the strategy opens or reverses to a long position. Conversely, when the moving average turns downward after increasing, it opens or reverses to a short position.

The logic mimics the original idea where a combined set of ten smoothed moving averages produces a single color-coded line. Here we replace that complex indicator with StockSharp's built-in `ZeroLagExponentialMovingAverage` to keep the implementation compact and reusable. The system works on the selected candle timeframe and can enable or disable individual actions (open/close long/short) via parameters.

## Details

- **Entry Criteria**:
  - **Long**: `ZLEMA[t-2] > ZLEMA[t-1]` and `ZLEMA[t] > ZLEMA[t-1]`.
  - **Short**: `ZLEMA[t-2] < ZLEMA[t-1]` and `ZLEMA[t] < ZLEMA[t-1]`.
- **Long/Short**: Both directions supported.
- **Exit Criteria**:
  - Long positions are closed when a short signal appears and `BuyPosClose` is enabled.
  - Short positions are closed when a long signal appears and `SellPosClose` is enabled.
- **Stops**: None by default; exits rely on opposite signals.
- **Default Values**:
  - `Length` = 20.
  - `CandleType` = 4-hour timeframe.
  - All action flags (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`) enabled.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Simple
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
