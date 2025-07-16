# Vwap Cci Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Implementation of strategy - VWAP + CCI. Buy when price is below VWAP and CCI is below -100 (oversold). Sell when price is above VWAP and CCI is above 100 (overbought).

VWAP acts as a value benchmark, and CCI highlights momentum moves away from it. Entries favor strong CCI readings relative to VWAP.

Designed for day traders focusing on VWAP interaction. ATR stops help maintain discipline.

## Details

- **Entry Criteria**:
  - Long: `Close < VWAP && CCI < CciOversold`
  - Short: `Close > VWAP && CCI > CciOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Price crosses back through VWAP
- **Stops**: Percent-based using `StopLoss`
- **Default Values**:
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: VWAP, CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
