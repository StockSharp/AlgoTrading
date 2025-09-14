# Arb Synthetic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy performs triangular arbitrage among EURUSD, GBPUSD, and EURGBP. It constructs synthetic prices to detect mispricing between the cross pair and its underlying legs. When discrepancies exceed a configurable spread threshold, the strategy opens market positions to exploit the divergence.

A long position buys the instrument when its market price is below the synthetic value; a short position sells when the market price is above the synthetic value. Positions are reversed when the spread closes.

## Details
- **Entry Criteria**:
  - **Buy**: Difference between synthetic and market price exceeds `Spread` in positive direction.
  - **Sell**: Difference between synthetic and market price exceeds `Spread` in negative direction.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal triggers position reversal.
- **Stops**: Uses built-in strategy protection.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `Spread` = 35
- **Filters**:
  - Category: Arbitrage
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium
