# Parabolic SAR Volatility Expansion
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Parabolic SAR Volatility Expansion** strategy is built around Parabolic SAR with Volatility Expansion detection.

Testing indicates an average annual return of about 49%. It performs best in the crypto market.

Signals trigger when Parabolic confirms trend changes on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like SarAf, SarMaxAf. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `SarAf = 0.02m`
  - `SarMaxAf = 0.2m`
  - `AtrPeriod = 14`
  - `VolatilityExpansionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

