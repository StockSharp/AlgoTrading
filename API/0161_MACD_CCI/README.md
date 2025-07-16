# Macd Cci Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Implementation of strategy #161 - MACD + CCI. Buy when MACD is above Signal line and CCI is below -100 (oversold). Sell when MACD is below Signal line and CCI is above 100 (overbought).

MACD swings highlight momentum shifts; CCI helps time pullback entries in that direction. Both long and short trades are possible.

Traders who blend momentum with oscillators may like this technique. Risk control uses an ATR stop.

## Details

- **Entry Criteria**:
  - Long: `MACD > Signal && CCI < CciOversold`
  - Short: `MACD < Signal && CCI > CciOverbought`
- **Long/Short**: Both
- **Exit Criteria**: MACD cross in opposite direction
- **Stops**: Percent-based using `StopLoss`
- **Default Values**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: MACD, CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
