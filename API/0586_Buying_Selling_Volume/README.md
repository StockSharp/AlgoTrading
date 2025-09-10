# Buying Selling Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses buying and selling volume distribution to detect pressure.
A long position is opened when buying volume dominates and the volume metric
breaks above a volatility band while price is above the weekly VWAP. A short position
uses the opposite conditions.

## Details

- **Entry Criteria**:
  - **Long**: Adjusted buying volume > adjusted selling volume, volume metric above upper band, close above weekly VWAP.
  - **Short**: Adjusted selling volume > adjusted buying volume, volume metric above upper band, close below weekly VWAP.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or ATR-based take profit/stop loss.
- **Stops**: ATR percentage multipliers via `ProfitTargetLong`, `StopLossLong`, `ProfitTargetShort`, `StopLossShort`.
- **Default Values**:
  - Length 20, StdDev 2.
  - ProfitTargetLong 100, StopLossLong 1.
  - ProfitTargetShort 100, StopLossShort 5.
- **Filters**:
  - Category: Volume based
  - Direction: Both
  - Indicators: Custom
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Medium-term
