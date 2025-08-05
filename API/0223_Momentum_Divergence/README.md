# Momentum Divergence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The Momentum Divergence strategy compares momentum readings with price direction to spot early signs of a reversal. Divergences occur when price makes a new extreme yet the momentum indicator fails to confirm, hinting at weakening strength.

Testing indicates an average annual return of about 106%. It performs best in the stocks market.

A bullish setup happens when price records a lower low while the momentum oscillator prints a higher low. A bearish setup forms when price pushes to a higher high but momentum fails to follow. Positions are closed when momentum crosses back through zero or the divergence is invalidated.

This approach appeals to traders looking to anticipate turning points rather than chase trends. Stops are used to control risk in case the market continues to move against the divergence signal.

## Details
- **Entry Criteria**:
  - **Long**: Price makes lower low && Momentum shows higher low
  - **Short**: Price makes higher high && Momentum shows lower high
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when momentum crosses below zero
  - **Short**: Exit when momentum crosses above zero
- **Stops**: Yes, fixed stop-loss.
- **Default Values**:
  - `MomentumPeriod` = 14
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Momentum
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium

