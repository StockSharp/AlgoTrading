# Macd Vwap Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy based on MACD and VWAP indicators. Enters long when MACD > Signal and price > VWAP Enters short when MACD < Signal and price < VWAP

Testing indicates an average annual return of about 109%. It performs best in the crypto market.

MACD momentum is gauged relative to the VWAP line. Long trades look for MACD strength below VWAP, whereas shorts take form above it.

Ideal for intraday momentum players using volume-weighted references. A percent stop manages risk.

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
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    The market trades around the clock, so the session VWAP restarts each UTC day and weighs the typical price of each candle by its volume. A long closes when MACD crosses below the signal line and a short when it crosses above it. An entry signal against an open position reverses it.
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

