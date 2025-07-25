# Low Volatility Reversion
[Русский](README_ru.md) | [中文](README_cn.md)
 
This mean-reversion strategy activates only during quiet markets. It measures ATR over a lookback window and enters when volatility falls below a percentage of that average and price deviates from its moving average.

By trading against small moves in calm conditions, it aims to capture snap backs without chasing large trends.

Positions exit once price touches the moving average or the ATR-based stop-loss is reached.

## Details

- **Entry Criteria**: Price away from moving average while ATR is below threshold.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price returns to MA or stop triggers.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrLookbackPeriod` = 20
  - `AtrThresholdPercent` = 50m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: ATR, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 139%. It performs best in the stocks market.
