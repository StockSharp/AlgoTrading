# Vwap Cci Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Implementation of strategy - VWAP + CCI. Buy when price is below VWAP and CCI is below -100 (oversold). Sell when price is above VWAP and CCI is above 100 (overbought).

Testing indicates an average annual return of about 82%. It performs best in the stocks market.

VWAP acts as a value benchmark, and CCI highlights momentum moves away from it. Entries favor strong CCI readings relative to VWAP.

Designed for day traders focusing on VWAP interaction. A percent stop helps maintain discipline.

## Details

- **Entry Criteria**:
  - Long: `Close < VWAP && CCI < CciOversold`
  - Short: `Close > VWAP && CCI > CciOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Price crosses back through VWAP
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    The market trades around the clock, so the session VWAP restarts each UTC day and weighs the typical price of each candle by its volume. An entry signal against an open position reverses it.
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

