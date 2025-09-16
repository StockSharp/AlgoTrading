# Global Take Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that monitors overall profit and closes all open positions once the profit target is reached.

## Details

- **Entry Criteria**: None. The strategy only monitors existing positions.
- **Long/Short**: Works with both long and short positions.
- **Exit Criteria**: Closes all positions when profit reaches the configured threshold.
- **Stops**: None.
- **Default Values**:
  - `Mode` = Percent
  - `TakeProfit` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Risk Management
  - Direction: Both
  - Indicators: None
  - Stops: None
  - Complexity: Basic
  - Timeframe: 1 Minute
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low

## How It Works

1. On start the strategy records the current portfolio value.
2. Every finished candle it checks the realized profit (PnL).
3. If profit is negative and the target is not reached, no action is taken.
4. When profit exceeds the set threshold (percent of initial value or absolute currency), all positions are closed with market orders.
5. Monitoring restarts once all positions are flat.
