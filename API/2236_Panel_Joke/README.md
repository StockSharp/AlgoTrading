# Panel Joke Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy converts the original MetaTrader *panel-joke* system into StockSharp. It compares the current candle with the previous one across seven price metrics (open, high, low, average of high and low, close, average of high/low/close, and weighted average of high/low/close). Each metric that increased counts toward a potential long setup; each decrease counts toward a short setup.

When the `Enable Autopilot` parameter is `true`, the strategy automatically opens or reverses positions based on which side has more points. No additional indicators or stop rules are used.

## Details

- **Entry Criteria**:
  - **Long**: Buy counter > Sell counter.
  - **Short**: Sell counter > Buy counter.
- **Exit Criteria**: Reverse when the opposite signal appears.
- **Stops**: None.
- **Default Values**:
  - `Enable Autopilot` = `true`.
  - `Candle Type` = 5-minute time frame.
- **Filters**:
  - Category: Price action
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High

