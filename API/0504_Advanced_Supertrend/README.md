# Advanced Supertrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Advanced Supertrend strategy enhances the classic Supertrend indicator with optional RSI, moving average, and trend-strength filters. It enters long when Supertrend flips to bullish and enters short when it turns bearish. Optional stop loss and take profit are derived from ATR multiples.

## Details

- **Entry Criteria**:
  - Supertrend changes direction (bearish→bullish for long, bullish→bearish for short).
  - Optional filters: RSI within set bounds, price relative to a moving average, trend strength, and breakout confirmation.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite Supertrend signal or optional stop-loss/take-profit levels.
- **Stops**: ATR-based optional stop loss and take profit.
- **Default Values**:
  - `AtrLength` = 6
  - `Multiplier` = 3.0
  - `UseRsiFilter` = false
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseMaFilter` = true
  - `MaLength` = 50
  - `MaType` = Weighted
  - `UseStopLoss` = true
  - `SlMultiplier` = 3.0
  - `UseTakeProfit` = true
  - `TpMultiplier` = 9.0
  - `UseTrendStrength` = false
  - `MinTrendBars` = 2
  - `UseBreakoutConfirmation` = true
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: Supertrend, RSI, Moving Average
  - Stops: ATR based
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
