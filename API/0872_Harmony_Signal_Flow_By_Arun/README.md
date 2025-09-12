# Harmony Signal Flow By Arun
[Русский](README_ru.md) | [中文](README_cn.md)

Harmony Signal Flow By Arun uses a short-period RSI to capture reversals with fixed stop-loss and target levels. The strategy goes long when RSI crosses above a lower threshold and short when it crosses below an upper threshold. Positions are closed by stop, target, or at 15:25 each day.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: RSI crosses above `LowerThreshold`.
  - **Short**: RSI crosses below `UpperThreshold`.
- **Exit Criteria**: Stop-loss or target hit, or 15:25 close.
- **Stops**: Fixed stop-loss and target.
- **Default Values**:
  - `RsiPeriod` = 5
  - `LowerThreshold` = 30
  - `UpperThreshold` = 70
  - `BuyStopLoss` = 100
  - `BuyTarget` = 150
  - `SellStopLoss` = 100
  - `SellTarget` = 150
- **Filters**:
  - Category: Mean reversion
  - Direction: Long & Short
  - Indicators: RSI
  - Complexity: Low
  - Risk level: Medium
