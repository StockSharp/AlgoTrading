# SuperTrend Enhanced Pivot Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines SuperTrend direction with pivot high/low breakouts. A long stop is placed above a recent pivot high when the SuperTrend is bearish. A short stop is placed below a pivot low when the SuperTrend is bullish. Positions are protected with a percentage stop-loss from the pivot.

## Details

- **Entry Criteria**:
  - Long: Pivot high formed, SuperTrend down → buy stop above pivot.
  - Short: Pivot low formed, SuperTrend up → sell stop below pivot.
- **Long/Short**: Configurable.
- **Exit Criteria**: Percentage stop-loss or opposite direction for single-side mode.
- **Indicators**: SuperTrend, pivot highs/lows.
- **Default Values**:
  - `LeftBars` = 6
  - `RightBars` = 3
  - `AtrLength` = 5
  - `Factor` = 2.618
  - `StopLossPercent` = 20
  - `TradeDirection` = Both
  - `CandleType` = 5 minute
