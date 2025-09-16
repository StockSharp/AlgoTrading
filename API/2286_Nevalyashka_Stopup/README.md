# Nevalyashka Stopup Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy alternates position direction after each trade, mimicking the "Nevalyashka" toy that flips sides. It uses a martingale approach: if a trade closes at a loss, the stop-loss and take-profit distances for the next trade are multiplied by a coefficient. After a profitable trade, distances reset to their base values and the strategy can optionally stop trading.

Initial direction is short. Every time a position is closed, the new position is opened in the opposite direction with the preconfigured volume.

## Details

- **Entry Criteria**:
  - First trade sells at market.
  - Subsequent trades always enter in the opposite direction of the previous closed trade.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Position is closed when price reaches the take-profit or stop-loss distance from entry.
- **Stops**: Yes, fixed stop-loss and take-profit in points. Distances grow by the martingale coefficient after losses.
- **Default Values**:
  - `StopLossPoints` = 150
  - `TakeProfitPoints` = 50
  - `OrderVolume` = 0.1
  - `MartingaleCoeff` = 1.5
  - `StopAfterProfit` = false
- **Filters**:
  - Category: Reversal / Martingale
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
