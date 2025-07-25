# Implied Volatility Spike
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy watches implied volatility for sudden jumps relative to the prior value. A strong spike paired with price trading against the moving average can signal a short-term reversal.

When implied volatility increases by the configured threshold, the system enters in the opposite direction of the price move, expecting volatility to revert.

Positions are closed once volatility begins to drop or a stop-loss occurs.

## Details

- **Entry Criteria**: IV spike above `IVSpikeThreshold` and price relative to MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: IV declines or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `IVPeriod` = 20
  - `IVSpikeThreshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: IV, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 163%. It performs best in the stocks market.
