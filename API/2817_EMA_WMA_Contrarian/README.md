# EMA WMA Contrarian Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Contrarian crossover system that compares an exponential moving average (EMA) and a weighted moving average (WMA) built on candle open prices. When the fast EMA slips below the WMA the strategy buys, betting on a snap-back. When the EMA climbs back above the WMA it enters short. Trade size is derived from the configured risk percentage and the distance to the protective stop, while optional stop-loss, take-profit, and trailing stop levels keep exposure under control.

## Details

- **Entry Criteria**:
  - Long: EMA(Open) crosses from above to below WMA(Open)
  - Short: EMA(Open) crosses from below to above WMA(Open)
- **Long/Short**: Both directions
- **Exit Criteria**:
  - Fixed stop-loss in price steps
  - Fixed take-profit in price steps
  - Trailing stop that advances after the price moves by `TrailingStopPoints + TrailingStepPoints`
  - Opposite crossover closes the current position and opens the new one
- **Stops**: Stop-loss, take-profit, and trailing stop
- **Default Values**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossPoints` = 50m
  - `TakeProfitPoints` = 50m
  - `TrailingStopPoints` = 50m
  - `TrailingStepPoints` = 10m
  - `RiskPercent` = 10m
  - `BaseVolume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Moving Average, Contrarian
  - Direction: Long & Short
  - Indicators: EMA (open), WMA (open)
  - Stops: Yes (hard stop, trailing)
  - Complexity: Intermediate
  - Timeframe: Intraday (1-minute default)
  - Seasonality: None
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Parameters

| Parameter | Description |
| --- | --- |
| `EmaPeriod`, `WmaPeriod` | Look-back periods for the EMA and WMA computed on candle opens. |
| `StopLossPoints`, `TakeProfitPoints` | Distance in price steps to place the protective stop-loss and profit target. |
| `TrailingStopPoints` | Distance between price and trailing stop once activated. |
| `TrailingStepPoints` | Additional favorable movement required before the trailing stop is pulled up/down. Must be positive when trailing is enabled. |
| `RiskPercent` | Percentage of portfolio equity risked per trade. Position size is computed as `RiskPercent / (StopLossPoints * PriceStep)`. |
| `BaseVolume` | Minimum trade size used when risk-based sizing cannot be determined. |
| `CandleType` | Candle data type for calculations (1-minute by default). |

## Notes

- Both moving averages consume candle open prices, mirroring the original MetaTrader expert advisor.
- Trailing stops only engage after price moves by at least `TrailingStopPoints + TrailingStepPoints` in favor of the trade, replicating the legacy logic.
- If `TrailingStopPoints` is set while `TrailingStepPoints` is zero or negative, the strategy stops immediately to avoid inconsistent trailing behaviour.
- Risk-based sizing falls back to `BaseVolume` if the portfolio value, price step, or stop distance are unavailable.
