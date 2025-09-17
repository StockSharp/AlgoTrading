[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the "Awesome Osc Trader" MetaTrader expert by combining Bollinger Band width, a stochastic filter and a normalized Awesome Oscillator momentum check. Long trades are opened when the oscillator is climbing from a negative extreme while the stochastic leaves the oversold area and market volatility stays inside a configurable band width. Shorts require the mirror conditions. A configurable trading window limits new orders to specific hours, and open positions can be force-closed on opposite signals only if the floating profit matches the chosen filter.

## Details

- **Entry Criteria**:
  - The Bollinger Band spread, converted to pips, must stay between `BollingerSpreadLowerLimit` and `BollingerSpreadUpperLimit`.
  - The stochastic main line is above `StochLower` for longs or below `StochUpper` for shorts.
  - The normalized Awesome Oscillator has shown at least four consecutive bars on the opposite side of zero and is turning back toward zero with strength above `AoStrengthLimit`.
  - Current time is inside the trading window defined by `EntryHour` and `OpenHours`.
- **Long/Short**: Trades both directions.
- **Exit Criteria**:
  - Optional early exit when an opposite signal appears or when the oscillator crosses zero, controlled by `CloseTrade` and `ProfitTypeClTrd`.
  - Protective stop-loss, take-profit and trailing stop distances supplied in pips.
- **Stops**: Fixed stop, take-profit and optional trailing stop managed through `StartProtection`.
- **Default Values**:
  - `BollingerPeriod` = 20, `BollingerSigma` = 2
  - `BollingerSpreadLowerLimit` = 55, `BollingerSpreadUpperLimit` = 380
  - `PeriodFast` = 3, `PeriodSlow` = 32
  - `AoStrengthLimit` = 0.13
  - `StochK` = 8, `StochD` = 3, `StochSlow` = 3
  - `StochLower` = 18, `StochUpper` = 76
  - `EntryHour` = 0, `OpenHours` = 16
  - `Lots` = 0.01, `TakeProfit` = 200, `StopLoss` = 80, `TrailingStop` = 40
  - `CloseTrade` = true, `ProfitTypeClTrd` = 1 (close only profitable positions)
- **Filters**:
  - Category: Momentum with volatility filter
  - Direction: Long and short
  - Indicators: Bollinger Bands, Stochastic Oscillator, Awesome Oscillator
  - Stops: Yes (fixed and trailing)
  - Complexity: Medium
  - Timeframe: Designed for H1 but works with any candle series
  - Seasonality: Trading-hour window
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
