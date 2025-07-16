# Donchian Rsi Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy combining Donchian Channels and RSI indicators. Buys on Donchian breakouts when RSI confirms trend is not overextended.

Donchian channels identify breakout levels, while RSI checks whether momentum supports the move. Positions follow when a breakout aligns with RSI direction.

Best for traders expecting a sustained breakout rather than a fakeout. Risk is limited through an ATR stop.

## Details

- **Entry Criteria**:
  - Long: `Close > DonchianHigh && RSI < RsiOversoldLevel`
  - Short: `Close < DonchianLow && RSI > RsiOverboughtLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - Breakout failure or opposite signal
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `DonchianPeriod` = 20
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
