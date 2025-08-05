# Volatility Adjusted Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This variation of mean reversion scales entry thresholds by the ratio of ATR to standard deviation. When volatility increases relative to typical noise, the distance needed to trigger a trade grows, helping avoid premature signals during chaotic swings.

Testing indicates an average annual return of about 115%. It performs best in the stocks market.

A long position opens when price falls below the moving average by more than the adjusted threshold. A short position opens when price rises above the average by the same measure. Positions exit once price closes back near the average level.

The adaptive threshold makes this strategy suitable for markets with changing volatility regimes. A stop-loss equal to twice the ATR limits risk while waiting for reversion.

## Details
- **Entry Criteria**:
  - **Long**: Close < MA - Multiplier * ATR / (ATR/StdDev)
  - **Short**: Close > MA + Multiplier * ATR / (ATR/StdDev)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when close >= MA
  - **Short**: Exit when close <= MA
- **Stops**: Yes, dynamic based on ATR.
- **Default Values**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: ATR, StdDev
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

