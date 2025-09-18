# DT RSI EXP1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This port replicates the MT4 expert advisor **DT-RSI-EXP1**. The strategy scans 15-minute RSI swings to detect double-tops or double-bottoms around the 60/40 levels. A long trade is taken when the recent RSI peaks pull back without printing any troughs below 40, while the 4-hour trend filter points down. Shorts mirror the logic with troughs above 60 and a rising trend filter. A fixed stop-loss and take-profit are attached to every position, and an optional trailing stop protects profits. Positions are force-closed when RSI stretches to extreme 70/30 levels, copying the original exit behaviour.

## Details

- **Entry Criteria**:
  - **Long**: two bullish RSI peaks with the second above 60, no bearish troughs below 40 in between, 4-hour EMA below the previous close, RSI(1) crossing above the projected neckline, RSI(2) still below it, RSI(2) < 50 and RSI(0) < 55.
  - **Short**: two bearish RSI troughs with the second below 40, no bullish peaks above 60 in between, 4-hour EMA above the previous close, RSI(1) crossing below the projected neckline, RSI(2) > 50 and RSI(0) > 47.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - RSI extremes (RSI > 70 for longs, RSI < 30 for shorts).
  - Stop-loss / take-profit targets computed from price steps.
  - Optional trailing stop that locks profits once price moves by `TrailingStopPoints`.
- **Stops**: Fixed stop-loss and take-profit, optional trailing stop.
- **Default Values**:
  - `CandleType` = 15-minute candles.
  - `TrendCandleType` = 240-minute candles (trend filter EMA).
  - `RsiPeriod` = 47.
  - `StopLossPoints` = 26.
  - `TakeProfitPoints` = 76.
  - `TrailingStopPoints` = 0 (disabled).
- **Filters**:
  - Category: Trend-Following Entries on RSI Structures.
  - Direction: Both.
  - Indicators: RSI, EMA trend filter.
  - Stops: Yes.
  - Complexity: Intermediate (multi-constraint swing detection).
  - Timeframe: Intraday (M15 with H4 filter).
  - Seasonality: No.
  - Neural Networks: No.
  - Divergence: No.
  - Risk Level: Medium.

## Parameters

| Name | Default | Description | Optimizable |
| ---- | ------- | ----------- | ----------- |
| `CandleType` | 15-minute | Primary candle series used to compute RSI and signals. | Yes |
| `TrendCandleType` | 240-minute | Higher timeframe used by the EMA trend filter (replacement for the MT4 RFTL indicator). | Yes |
| `RsiPeriod` | 47 | RSI length applied to the primary candles. | Yes |
| `StopLossPoints` | 26 | Distance to the stop-loss in price steps. | Yes |
| `TakeProfitPoints` | 76 | Distance to the take-profit in price steps. | Yes |
| `TrailingStopPoints` | 0 | Trailing-stop offset in price steps (`0` disables trailing). | Yes |

## Notes

- The MetaTrader `RFTL` custom indicator is approximated with a 10-period EMA on the 240-minute timeframe. Adjust the higher timeframe or EMA length to better match the original environment.
- Ensure the instrument's `PriceStep` and `StepPrice` are configured so that point-based stops align with the broker's tick size.
- The trailing stop only activates once price advances by more than `TrailingStopPoints` from the entry price and never loosens beyond the original stop.
