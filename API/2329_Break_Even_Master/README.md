# Break Even Master
[Русский](README_ru.md) | [中文](README_cn.md)

The **Break Even Master** strategy automatically moves the stop loss to the entry price once a trade gains a specified number of ticks. It can optionally filter the orders by comment and magic number, replicating the behavior of the original MetaTrader expert advisor.

## Details
- **Entry Criteria**: External, the strategy only manages existing positions.
- **Long/Short**: Both.
- **Exit Criteria**: Price hits the break-even stop.
- **Stops**: Break-even only.
- **Default Values**:
  - `BreakEvenTicks = 20`
  - `UseComment = false`
  - `Comment = ""`
  - `UseMagicNumber = false`
  - `MagicNumber = 12345`
  - `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: None
  - Stops: Break-even
  - Complexity: Beginner
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
