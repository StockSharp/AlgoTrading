# MFI Strategy with Oversold Zone Exit and Averaging
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy waits for the Money Flow Index (MFI) to enter the oversold zone. Once MFI rises above the oversold level, it places a limit buy order a fixed percentage below the current close. If the order is not filled within a specified number of bars, it is canceled. Stop-loss and take-profit are applied via StartProtection.

## Details

- **Entry Criteria**:
  - MFI climbs above `MfiOversoldLevel` after being below it; place limit buy at `LongEntryPercentage` below close.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Position closed by take-profit or stop-loss (`ExitGainPercentage`, `StopLossPercentage`).
- **Stops**: Yes, via StartProtection.
- **Default Values**:
  - `MfiPeriod` = 14
  - `MfiOversoldLevel` = 20
  - `LongEntryPercentage` = 0.1
  - `StopLossPercentage` = 1
  - `ExitGainPercentage` = 1
  - `CancelAfterBars` = 5
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: MFI
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
