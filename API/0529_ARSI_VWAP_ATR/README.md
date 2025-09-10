# Arsi Vwap Atr Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive RSI strategy where overbought and oversold levels expand or contract based on ATR or the deviation from VWAP. Positions are opened on RSI crossovers of the adaptive levels and closed when RSI returns to the mid-zone.

## Details

- **Entry Criteria**:
  - Long: `RSI` crosses above adaptive oversold line
  - Short: `RSI` crosses below adaptive overbought line
- **Long/Short**: Both
- **Exit Criteria**:
  - RSI crosses back through 50 or opposite adaptive line
- **Stops**: Percent-based using `StopLossPercent` and `RiskReward`
- **Default Values**:
  - `RsiLength` = 14
  - `BaseK` = 1m
  - `RiskPercent` = 2m
  - `StopLossPercent` = 2.5m
  - `RiskReward` = 2m
  - `SourceOb` = ATR
  - `SourceOs` = ATR
  - `AtrLengthOb` = 14
  - `AtrLengthOs` = 14
  - `ObMultiplier` = 10m
  - `OsMultiplier` = 10m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI, ATR, VWAP
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
