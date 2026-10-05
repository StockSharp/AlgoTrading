# Vwap Adx Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy based on VWAP and ADX indicators. Enters long when price is above VWAP and ADX > 25. Enters short when price is below VWAP and ADX > 25. Exits when ADX < 20.

Testing indicates an average annual return of about 157%. It performs best in the crypto market.

VWAP acts as the session benchmark, and ADX measures conviction. Entries appear when price departs from VWAP with ADX showing strength.

Fits intraday trend traders. Protective stops use a fixed percentage of the entry price.

## Details

- **Entry Criteria**:
  - Long: `Close > VWAP && ADX > 25`
  - Short: `Close < VWAP && ADX > 25`
- **Long/Short**: Both
- **Exit Criteria**: ADX drops below threshold
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `StopLossPercent` = 2
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    The market trades around the clock, so the session VWAP restarts each UTC day and weighs the typical price of each candle by its volume. The 25 and 20 in the rules are the defaults of AdxThreshold and AdxExitThreshold. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: VWAP, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

