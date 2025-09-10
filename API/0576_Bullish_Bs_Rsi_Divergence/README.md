# Bullish B's RSI Divergence
[Русский](README_ru.md) | [中文](README_cn.md)

Uses RSI to detect regular and hidden bullish divergences with pivot points. Opens long trades on divergence and closes on bearish signals, RSI target, or trailing stop.

## Details

- **Entry Criteria**:
  - **Long**: Regular or hidden bullish RSI divergence.
- **Long/Short**: Long only.
- **Exit Criteria**: Bearish divergence, RSI crossing above target, or trailing stop.
- **Stops**: Optional trailing stop based on ATR or percent.
- **Default Values**:
  - `RsiPeriod` = 9
  - `PivotLookbackRight` = 3
  - `PivotLookbackLeft` = 1
  - `TakeProfitRsiLevel` = 80
  - `RangeUpper` = 60
  - `RangeLower` = 5
  - `StopType` = None
  - `StopLoss` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 3.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Divergence
  - Direction: Long
  - Indicators: RSI, ATR
  - Stops: Optional trailing stop
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
