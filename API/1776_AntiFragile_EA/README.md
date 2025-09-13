# AntiFragile EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Grid strategy placing layered limit orders above and below current price with increasing volume.
Positions are protected by an initial stop and trailed as price moves favorably.

## Details

- **Entry Criteria**:
  - Long: Place buy limit orders every `SpaceBetweenTrades` steps below bid.
  - Short: Place sell limit orders every `SpaceBetweenTrades` steps above ask.
- **Long/Short**: Optional for each side via `TradeLong` and `TradeShort`.
- **Exit Criteria**: Trailing stop or opposite grid side execution.
- **Stops**: Initial `StopLossPips` and trailing via `TrailingStopPips`.
- **Default Values**:
  - `StartingVolume` = 0.1m
  - `IncreasePercentage` = 1m
  - `SpaceBetweenTrades` = 700m
  - `NumberOfTrades` = 50
  - `StopLossPips` = 300m
  - `TrailingStopPips` = 100m
  - `TradeLong` = true
  - `TradeShort` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Grid trading
  - Direction: Both
  - Indicators: None
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
