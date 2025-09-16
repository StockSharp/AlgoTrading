# Trend Catcher Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Trend Catcher** strategy combines Parabolic SAR with multiple simple moving averages to capture directional moves. It waits for price to cross the Parabolic SAR in the direction of the prevailing fast averages and then manages the position using dynamic stop-loss and trailing rules.

A trade is opened when the latest candle closes on the opposite side of the Parabolic SAR compared to the previous candle while fast averages confirm the move. The initial stop-loss is calculated from the distance to the SAR point and is bounded by minimum and maximum limits. Profit targets are defined as a multiple of the stop distance. After price advances by a specified amount, the stop moves to breakeven with a small offset and later trails the price.

## Details

- **Entry Criteria**:
  - **Long**: `Close[0] > SAR && Close[1] < SAR_prev && FastMA > SlowMA && Close > FastMA2`.
  - **Short**: `Close[0] < SAR && Close[1] > SAR_prev && FastMA < SlowMA && Close < FastMA2`.
- **Exit Criteria**:
  - Stop-loss or take-profit levels are hit.
  - Trailing stop activated after profit threshold.
  - Opposite signal closes existing position.
- **Stops**: Dynamic stop-loss from SAR with optional breakeven and trailing adjustments.
- **Default Values**:
  - `SlowMaPeriod = 200`
  - `FastMaPeriod = 50`
  - `FastMa2Period = 25`
  - `SarStep = 0.004`
  - `SarMax = 0.2`
  - `SlMultiplier = 1`
  - `TpMultiplier = 1`
  - `MinStopLoss = 10`
  - `MaxStopLoss = 200`
  - `ProfitLevel = 500`
  - `BreakevenOffset = 1`
  - `TrailingThreshold = 500`
  - `TrailingDistance = 10`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic SAR, SMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
