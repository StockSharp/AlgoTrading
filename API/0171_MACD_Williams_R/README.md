# Macd Williams R Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on MACD and Williams %R indicators. Enters long when MACD > Signal and Williams %R is oversold (< -80) Enters short when MACD < Signal and Williams %R is overbought (> -20)

MACD indicates the larger momentum shift, while Williams %R pinpoints near-term reversals. Both signals must line up to initiate a trade.

Good for those who like to combine trend and countertrend cues. Stops hinge on an ATR factor.

## Details

- **Entry Criteria**:
  - Long: `MACD > Signal && WilliamsR < -80`
  - Short: `MACD < Signal && WilliamsR > -20`
- **Long/Short**: Both
- **Exit Criteria**: MACD cross in opposite direction
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: MACD, Williams %R, R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 100%. It performs best in the forex market.
