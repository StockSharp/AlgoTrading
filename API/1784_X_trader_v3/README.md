# X Trader V3 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades crossovers between two median price moving averages. The first moving average is longer and shifted, while the second is short. A long position is opened when the first moving average crosses below the second and remains below for two bars after being above two bars ago. A short position is opened on the opposite crossover. Positions can be closed on reverse signals. Trading is limited to a specified intraday time window. Optional protective stops are available.

## Details

- **Entry Criteria**:
  - Median price SMA(`Ma1Period`) crosses below median price SMA(`Ma2Period`) and stays below for two bars ⇒ buy when `AllowBuy` is true.
  - Median price SMA(`Ma1Period`) crosses above median price SMA(`Ma2Period`) and stays above for two bars ⇒ sell when `AllowSell` is true.
  - Candle time between `StartTime` and `EndTime`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover when `CloseOnReverseSignal` is true.
- **Stops**:
  - Optional take profit and stop loss in ticks via `TakeProfitTicks` and `StopLossTicks`.
- **Default Values**:
  - `Ma1Period` = 16
  - `Ma2Period` = 1
  - `TakeProfitTicks` = 150
  - `StopLossTicks` = 100
- **Filters**:
  - Category: Crossover
  - Direction: Both
  - Indicators: SMA
  - Stops: Optional
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
