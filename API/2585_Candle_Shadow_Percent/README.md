# Candle Shadow Percent Strategy

## Overview
The **Candle Shadow Percent Strategy** is a direct port of the MetaTrader expert advisor *Candle shadow percent*. It searches for candles where the upper or lower wick reaches a configurable percentage of the candle body. When a tall upper wick appears the strategy opens a short position; when a deep lower wick appears it opens a long position. The trade direction is aligned with the original algorithm and keeps the risk management workflow intact.

## Conversion Notes
* The original expert depended on a custom indicator. In the StockSharp version the wick and body proportions are calculated directly from finished candles, so there are no external indicator dependencies.
* Pip values are derived from `Security.PriceStep`. Adjust `StopLossPips`, `TakeProfitPips`, and `MinBodyPips` to match the instrument tick size.
* Risk-based position sizing mirrors the MetaTrader `CMoneyFixedMargin` logic by risking a percentage of the current portfolio value against the configured stop-loss distance.

## Candle Qualification
A candle is considered for trading when:
1. Its absolute body size is at least `MinBodyPips * Security.PriceStep`.
2. The corresponding wick is positive.
3. The wick-to-body ratio satisfies the selected threshold logic:
   * **Upper wick** (sell setup): `(High − max(Open, Close)) / Body * 100` is greater than or equal to `TopShadowPercent` when `TopShadowIsMinimum = true`, otherwise it must be less than or equal to that value.
   * **Lower wick** (buy setup): `(min(Open, Close) − Low) / Body * 100` is greater than or equal to `LowerShadowPercent` when `LowerShadowIsMinimum = true`, otherwise it must be less than or equal to that value.
4. When both wicks satisfy their thresholds in the same candle, the strategy keeps only the side with the larger wick ratio to avoid double signals.

## Entry Rules
* **Short entry** – triggered on a valid upper wick signal while the strategy is flat or long. The strategy reverses existing long exposure if required and sets the protective orders immediately.
* **Long entry** – triggered on a valid lower wick signal while the strategy is flat or short. Existing short exposure is closed automatically before establishing the new long position.

## Exit Rules
* **Stop-loss** – placed at `StopLossPips * Security.PriceStep` away from the entry price. Long positions use `entry − stopDistance`; short positions use `entry + stopDistance`.
* **Take-profit** – optional target located at `TakeProfitPips * Security.PriceStep` from entry. When `TakeProfitPips = 0` the target is disabled and positions rely solely on the stop-loss or opposite signal to exit.
* The strategy monitors completed candles. If a candle range touches the stop or target, the position is closed on the next processing cycle.

## Position Sizing
* Risk per trade is calculated as `Portfolio.CurrentValue * (RiskPercent / 100)`. If the portfolio value is unavailable the strategy falls back to the configured strategy volume.
* Quantity equals the risk amount divided by the stop-loss distance. When reversing, the algorithm adds the absolute size of the current exposure to ensure a full reversal, matching the behaviour of the original MetaTrader expert.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe or data type used for candle subscriptions. |
| `StopLossPips` | Stop-loss distance expressed in pips/ticks relative to the instrument. Must be greater than zero. |
| `TakeProfitPips` | Take-profit distance in pips/ticks. Use zero to disable the target. |
| `RiskPercent` | Percentage of portfolio value risked per trade. |
| `MinBodyPips` | Minimum candle body size (in pips/ticks) required before evaluating wick ratios. |
| `EnableTopShadow` | Enables short signals based on upper shadow length. |
| `TopShadowPercent` | Threshold percentage for the upper wick-to-body ratio. |
| `TopShadowIsMinimum` | When true, the ratio must be greater than or equal to the threshold; when false, it must be less than or equal to it. |
| `EnableLowerShadow` | Enables long signals based on lower shadow length. |
| `LowerShadowPercent` | Threshold percentage for the lower wick-to-body ratio. |
| `LowerShadowIsMinimum` | Controls whether the lower wick threshold is treated as a minimum or maximum condition. |

## Usage Tips
* Start with a timeframe similar to the original EA (e.g., 5-minute candles) and adjust pip distances for your instrument.
* Increase `MinBodyPips` if noise produces too many signals; decrease it to catch smaller reversals.
* Combine the strategy with additional filters (such as trend indicators) by extending the class—bindings for extra indicators can be added inside `OnStarted`.
* Always validate tick size interpretation on a demo portfolio before deploying to production.
