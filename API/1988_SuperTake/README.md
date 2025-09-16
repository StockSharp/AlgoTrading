# Super Take Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy alternates between long and short positions and increases the take profit after each losing trade using a martingale multiplier. The stop loss is fixed while the take profit resets to the base value after a winning trade. By always flipping direction and adjusting targets after losses, the strategy attempts to recover previous drawdowns.

A new position is opened only when no position is active. The first trade is long by default. Each subsequent trade opens in the opposite direction of the last closed position.

## Details

- **Entry Criteria**:
  - **Long**: No active position and the last closed position was short or absent.
  - **Short**: No active position and the last closed position was long.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Close the position when price reaches the dynamic take profit or the fixed stop loss.
- **Stops**: Fixed stop loss, dynamic take profit with martingale after losing trades.
- **Default Values**:
  - `TakeProfit` = 10
  - `StopLoss` = 15
  - `MartinFactor` = 1.8
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
