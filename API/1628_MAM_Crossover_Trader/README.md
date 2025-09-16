# MAM Crossover Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy built on comparing simple moving averages of candle close and open prices.
A long signal occurs when the close SMA crosses above the open SMA and the previous bar confirmed a transition from below. A short signal appears on the opposite pattern. Opposite positions are closed on signal reversal. Optional fixed stop-loss and take-profit protect trades.

## Details

- **Entry Criteria**: Pattern of SMA(close) and SMA(open) crossovers over the last two bars.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover or protective stops.
- **Stops**: Yes.
- **Default Values**:
  - `MaPeriod` = 20
  - `StopLossTicks` = 40
  - `TakeProfitTicks` = 190
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA
  - Stops: Fixed
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
