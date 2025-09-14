# Parallel Strategies
[Русский](README_ru.md) | [中文](README_cn.md)

Heikin Ashi MACD breakout system trading both directions. It enters when a new Heikin Ashi trend aligns with a breakout above or below the Donchian Channel and the MACD confirms momentum.

Combining trend identification from Heikin Ashi with breakout detection keeps trades aligned with fresh moves. MACD acts as a momentum filter to avoid false signals.

Best for traders looking for early breakout entries after a trend reversal. Works on intraday timeframes.

## Details

- **Entry Criteria**:
  - Long: `Trend turns bullish && Close > DonchianHigh && MACD > Signal`
  - Short: `Trend turns bearish && Close < DonchianLow && MACD < Signal`
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite breakout signal
- **Stops**: Not defined
- **Default Values**:
  - `DonchianPeriod` = 5
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Heikin Ashi, Donchian Channel, MACD
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
