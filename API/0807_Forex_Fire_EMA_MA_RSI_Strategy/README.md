# Forex Fire EMA MA RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-timeframe trend strategy using EMA, MA and RSI confirmation. Uses 4h candles for confluence and 15m candles for entries.

## Details

- **Entry Criteria**:
  - Long: Short EMA above long EMA, price above MA, fast RSI above slow RSI and >50, volume increasing with higher timeframe confirmation.
  - Short: Opposite conditions.
- **Long/Short**: Both.
- **Exit Criteria**:
  - EMA cross or RSI reaching thresholds.
  - Optional stop loss, take profit, trailing stop and ATR-based exit.
- **Stops**: Yes, configurable.
- **Default Values**:
  - `EmaShortLength` = 13
  - `EmaLongLength` = 62
  - `MaLength` = 200
  - `MaType` = MovingAverageTypeEnum.Simple
  - `RsiSlowLength` = 28
  - `RsiFastLength` = 7
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
  - `UseTrailingStop` = true
  - `TrailingPercent` = 1.5
  - `UseAtrExits` = true
  - `AtrMultiplier` = 2
  - `AtrLength` = 14
  - `EntryCandleType` = TimeSpan.FromMinutes(15).TimeFrame()
  - `ConfluenceCandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, MA, RSI, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
