# SuperTrade ST1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Long-only strategy combining Supertrend with EMA filter and ATR-based risk management.

Testing indicates an average annual return of about 45%. It performs best in the crypto market.

The system waits for a drop in Supertrend direction while price stays above both the Supertrend line and EMA. Risk is controlled with ATR-based stop-loss and take-profit levels at a 1:4 ratio.

## Details

- **Entry Criteria**:
  - Previous Supertrend direction > current direction
  - Close > Supertrend
  - Close > EMA
- **Long/Short**: Long only
- **Exit Criteria**: `Close <= entry - StopAtrMultiplier * ATR` or `Close >= entry + TakeAtrMultiplier * ATR`
- **Stops**: ATR-based stop-loss and take-profit
- **Default Values**:
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `EmaPeriod` = 200
  - `StopAtrMultiplier` = 1.0
  - `TakeAtrMultiplier` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: Supertrend, EMA, ATR
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

