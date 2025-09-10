# ADX CCI MA
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combines ADX, CCI and a configurable moving average to trade strong trends.

The system buys when +DI crosses above -DI, CCI > 100 and ADX exceeds the threshold (optionally close above MA). It sells short when -DI crosses above +DI, CCI < -100 and ADX exceeds the threshold (close below MA).

Includes percentage-based stop-loss and take-profit plus optional MA risk management that exits after several candles close against the moving average.

## Details

- **Entry Criteria**: +DI/-DI cross with CCI extreme and ADX > `AdxThreshold`, optional close vs MA.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss or take-profit hit, optional MA risk management.
- **Stops**: Yes, take profit and stop loss.
- **Default Values**:
  - `EnableLong` = true
  - `EnableShort` = true
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CciPeriod` = 15
  - `AdxLength` = 10
  - `AdxThreshold` = 20m
  - `UseMaTrend` = true
  - `MaType` = MovingAverageTypeEnum.Simple
  - `MaLength` = 200
  - `UseMaRiskManagement` = false
  - `MaRiskExitCandles` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ADX, CCI, MA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
