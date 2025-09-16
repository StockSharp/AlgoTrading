# Drawdown Close All Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy monitors account equity and closes all open positions when the drawdown from peak equity exceeds a specified percentage. It can serve as a safety net to protect capital during large losses.

## Details

- **Entry Criteria**: None
- **Long/Short**: Both
- **Exit Criteria**: Drawdown ≥ `MaxDrawdownPercent`
- **Stops**: No
- **Default Values**:
  - `MaxDrawdownPercent` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Depends on current positions
