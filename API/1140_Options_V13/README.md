# Options Strategy V1.3
[Русский](README_ru.md) | [中文](README_cn.md)

An EMA crossover strategy with RSI, ATR-based stop and take-profit, and volume filter. The system can optionally require a breakout of the opening range and closes positions at 15:55 New York time. Trading is blocked during predefined sessions and a user-specified no-trade interval.

## Details

- **Entry Criteria**:
  - **Long**: short EMA crosses above long EMA, RSI ≥ `RsiLongThreshold`, volume ≥ volume SMA, optional close > OR high.
  - **Short**: short EMA crosses below long EMA, RSI ≤ `RsiShortThreshold`, volume ≥ volume SMA, optional close < OR low.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - ATR-based stop-loss and take-profit.
  - Opposite EMA cross.
  - Auto close at 15:55 NY time.
- **Stops**: Yes.
- **Default Values**:
  - `EmaShortLength = 8`
  - `EmaLongLength = 28`
  - `RsiLength = 12`
  - `AtrLength = 14`
  - `SlMultiplier = 1.4`
  - `TpSlRatio = 4`
  - `VolumeMaLength = 20`
- **Filters**:
  - Category: Trend following
  - Direction: Configurable
  - Indicators: EMA, RSI, ATR, SMA
  - Stops: Yes
  - Timeframe: Intraday
