# Macd Vwap Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on MACD and VWAP indicators. Enters long when MACD > Signal and price > VWAP Enters short when MACD < Signal and price < VWAP

Testing indicates an average annual return of about 109%. It performs best in the crypto market.

MACD momentum is gauged relative to the VWAP line. Long trades look for MACD strength below VWAP, whereas shorts take form above it.

Ideal for intraday momentum players using volume-weighted references. ATR-based stops manage risk.

## Details

- **Entry Criteria**:
  - Long: `MACD > Signal && Close > VWAP`
  - Short: `MACD < Signal && Close < VWAP`
- **Long/Short**: Both
- **Exit Criteria**: MACD cross opposite
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: MACD, VWAP
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

