# 318 Williams R Momentum
The **Williams R Momentum** strategy is built around Williams %R with Momentum filter.

Signals trigger when Williams confirms momentum shifts on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like WilliamsRPeriod, MomentumPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `WilliamsRPeriod = 14`
  - `MomentumPeriod = 14`
  - `WilliamsROversold = -80m`
  - `WilliamsROverbought = -20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Williams, R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
