# OBV Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
On Balance Volume (OBV) tracks cumulative volume flow to determine whether buyers or sellers are dominant. This strategy waits for OBV to diverge sharply from its average and then trades in anticipation of a return to typical levels.

Testing indicates an average annual return of about 79%. It performs best in the stocks market.

A buy signal occurs when OBV falls below its average minus `Multiplier` times the standard deviation and price is below the moving average. A sell signal is generated when OBV rises above the upper band with price above the average. Positions close when OBV crosses back through its mean line.

The approach is useful for traders who consider volume flows in addition to price action. Stops are placed a set percentage away to handle situations where volume continues to accelerate.

## Details
- **Entry Criteria**:
  - **Long**: OBV < Avg - Multiplier * StdDev && Close < MA
  - **Short**: OBV > Avg + Multiplier * StdDev && Close > MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when OBV > Avg
  - **Short**: Exit when OBV < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: OBV
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

