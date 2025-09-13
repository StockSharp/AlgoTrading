# Renko Scalper Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy attempts to capture short-term momentum by comparing the current close to the previous close.
If the latest candle closes higher than the prior one, the strategy opens a long position.
If the latest candle closes lower than the prior one, it opens a short position.

Stops and optional trailing stop are handled via the built-in protection module.
The approach works on both sides of the market and relies solely on price action.

## Details

- **Entry Criteria**:
  - **Long**: `Close(t) > Close(t-1)`.
  - **Short**: `Close(t) < Close(t-1)`.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or protective stops.
- **Stops**: Optional trailing stop, stop loss, and take profit via `StartProtection`.
- **Default Values**:
  - `CandleType` = 1-minute.
  - `StopLossPercent` = 1.
  - `TakeProfitPercent` = 2.
  - `IsTrailingStop` = true.
- **Filters**:
  - Category: Scalping.
  - Direction: Both.
  - Indicators: None.
  - Stops: Yes.
  - Complexity: Simple.
  - Timeframe: Short-term.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: High.
