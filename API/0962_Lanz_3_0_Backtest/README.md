# LANZ Strategy 3.0
[Русский](README_ru.md) | [中文](README_cn.md)

LANZ Strategy 3.0 trades breakouts of the Asian range. The direction is chosen after the 01:15–02:15 NY decision window and a limit order is placed at the range high or low with Fibonacci-based targets and stops. If the order is not filled by 02:15 it may flip direction. Unfilled orders are cancelled at 08:00 and open positions are closed at 15:45.

## Details

- **Entry Criteria**:
  - Breakout of the Asian range high or low after the decision window.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Fibonacci-based take profit or stop loss.
  - All positions closed at 15:45 NY.
- **Stops**: Fibonacci multipliers.
- **Default Values**:
  - `UseOptimizedFibo` = true
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: High
  - Timeframe: Any
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
