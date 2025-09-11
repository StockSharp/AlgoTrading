# ETH Signal 15m Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The ETH Signal 15m strategy uses the Supertrend indicator to detect direction changes and the RSI to filter entries. A long position opens when the Supertrend direction decreases and RSI is below the overbought level. A short position opens when the Supertrend direction increases and RSI is above the oversold level. Exits use ATR-based stop loss and take profit.

## Details

- **Entry Criteria**:
  - **Long**: Supertrend direction decreases and RSI below `RsiOverbought`.
  - **Short**: Supertrend direction increases and RSI above `RsiOversold`.
- **Long/Short**: Both sides.
- **Exit Criteria**: ATR-based stop loss and take profit.
- **Stops**: 4×ATR stop loss, 2×ATR long take profit, 2.237×ATR short take profit.
- **Default Values**:
  - `AtrPeriod` = 12
  - `Factor` = 2.76
  - `RsiLength` = 12
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Supertrend, RSI, ATR
  - Stops: ATR stop loss and take profit
  - Complexity: Low
  - Timeframe: 15m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
