# ATR Reversion
[Русский](README_ru.md) | [中文](README_cn.md)
 
ATR Reversion looks for sudden moves measured in multiples of Average True Range (ATR). When price surges beyond the ATR multiplier, the system expects a mean reversion.

The strategy opens a trade opposite the direction of the spike and uses a moving average to judge momentum.

Positions close on a moving-average crossover or when the volatility stop is hit.

## Details

- **Entry Criteria**: Price move exceeds `AtrMultiplier` times ATR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses moving average or stop.
- **Stops**: Yes.
- **Default Values**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: ATR, MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 133%. It performs best in the crypto market.
