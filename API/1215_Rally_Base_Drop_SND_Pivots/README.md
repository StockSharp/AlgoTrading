# Rally Base Drop SND Pivots Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Rally Base Drop SND Pivots strategy trades breakouts of supply and demand pivot levels. Pivots are detected when sequences of bullish and bearish candles form rally-base-drop or drop-base-rally patterns. When price crosses these pivot levels, a position is opened. Exits use an ATR-based stop and a risk-reward target.

## Details

- **Entry Criteria**:
  - **Long**: Price crosses above a pivot high (or pivot low when reversed).
  - **Short**: Price crosses below a pivot low (or pivot high when reversed).
- **Long/Short**: Configurable (long only, short only, or both).
- **Exit Criteria**:
  - Price hits ATR stop or risk-reward target.
- **Stops**: ATR multiplier with risk-reward target.
- **Default Values**:
  - `Length` = 3
  - `Mult` = 1.0
  - `RiskReward` = 6.0
  - `ReverseConditions` = false
- **Filters**:
  - Category: Support/resistance breakout
  - Direction: Both
  - Indicators: ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
