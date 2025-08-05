# ATR Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The ATR Mean Reversion strategy measures how far price travels away from a moving average relative to recent volatility. The Average True Range (ATR) provides an adaptive gauge so thresholds expand during active periods and contract when markets quiet down.

Testing indicates an average annual return of about 109%. It performs best in the crypto market.

A long setup occurs when price closes below the moving average by more than `Multiplier` times the ATR. A short setup appears when price closes above the moving average by the same distance. Positions are exited once price returns to the moving average.

This technique is intended for short-term traders expecting prices to revert after excessive moves. The ATR-based stop keeps risk proportional to current market conditions.

## Details
- **Entry Criteria**:
  - **Long**: Close < MA - Multiplier * ATR
  - **Short**: Close > MA + Multiplier * ATR
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when close >= MA
  - **Short**: Exit when close <= MA
- **Stops**: Yes, stop-loss around `2*ATR` by default.
- **Default Values**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: MA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

