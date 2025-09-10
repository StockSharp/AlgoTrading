# AUD/USD Scalping Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy scalps AUD/USD on short timeframes using a combination of EMA trend filter, Bollinger Bands and RSI. The fast and slow EMAs define trend direction. Long trades are opened in uptrends when price touches the lower Bollinger Band and RSI is above the oversold threshold. Shorts are taken in downtrends when price reaches the upper band and RSI is below the overbought level. Fixed take profit and stop loss manage risk.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA above slow EMA, price at or below lower Bollinger Band, RSI above oversold level.
  - **Short**: Fast EMA below slow EMA, price at or above upper Bollinger Band, RSI below overbought level.
- **Long/Short**: Both sides.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Fixed stop loss and take profit.
- **Default Values**:
  - `EmaShort` = 13
  - `EmaLong` = 26
  - `RsiPeriod` = 4
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `TakeProfit` = 0.0005
  - `StopLoss` = 0.0004
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: EMA, Bollinger Bands, RSI
  - Stops: Fixed
  - Complexity: Low
  - Timeframe: 1 minute
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
