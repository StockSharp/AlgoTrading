# Three Neural Networks Strategy

## Overview

This strategy is a high-level StockSharp port of the MetaTrader expert advisor "Three neural networks". It works entirely through the StockSharp candle subscription API and reuses built-in `SmoothedMovingAverage` indicators to emulate the three neural layers from the original implementation. The strategy operates on three different timeframes (H1, H4, D1) and analyses the slope of each smoothed average to derive a collective trading decision.

## Workflow

1. When the strategy starts it subscribes to H1, H4 and D1 timeframe candles and binds smoothed moving averages that use the median price, mirroring the `iMA(..., MODE_SMMA, PRICE_MEDIAN)` calls from MetaTrader.
2. Each timeframe maintains a rolling history that respects the configured shift. Once four shifted values are available the algorithm calculates three neural outputs using exactly the same weighted difference formula as the EA and rounds the result to four decimal places.
3. After the H1 candle is finished the strategy combines the neural outputs:
   - If all three values are positive → open or maintain a long position.
   - If the H1 output is positive while the H4 and D1 outputs are negative → open or maintain a short position.
4. Positions are sized with either a fixed lot or a risk-percentage model. In risk mode the strategy allocates `VolumeOrRisk` percent of the portfolio value and converts it to volume by dividing by the current price.
5. Protective logic replicates the EA controls: a stop-loss and take-profit are placed in local variables immediately after the position changes, and a trailing stop is tightened every time the H1 bar closes if price advances beyond the trailing distance plus the configured step.
6. Each finished H1 candle first checks whether the current stop-loss or take-profit levels are breached and closes the position with a market order if needed. Optional verbose logging reproduces the original `InpPrintLog` flag.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `StopLossPips` | `50` | Protective stop distance in pips. Set to `0` to disable the stop-loss. |
| `TakeProfitPips` | `50` | Take-profit distance in pips. Set to `0` to disable the target. |
| `TrailingStopPips` | `15` | Distance between the current price and the trailing stop. |
| `TrailingStepPips` | `5` | Minimum improvement required before moving the trailing stop again. |
| `ManagementMode` | `RiskPercent` | Volume sizing mode. `FixedLot` uses the value as a direct lot size; `RiskPercent` uses it as a percentage of the portfolio equity. |
| `VolumeOrRisk` | `1` | Lot size or risk percentage, depending on the money management mode. |
| `H1Period`, `H1Shift` | `2`, `5` | Period and shift of the H1 smoothed moving average. |
| `H4Period`, `H4Shift` | `2`, `5` | Period and shift of the H4 smoothed moving average. |
| `D1Period`, `D1Shift` | `2`, `5` | Period and shift of the D1 smoothed moving average. |
| `P1`, `P2`, `P3` | `0.1` | Weights applied to the three H1 neural components. |
| `Q1`, `Q2`, `Q3` | `0.1` | Weights applied to the three H4 neural components. |
| `K1`, `K2`, `K3` | `0.1` | Weights applied to the three D1 neural components. |
| `EnableDetailedLog` | `false` | Enables verbose diagnostic messages that mirror the EA log output. |

## Risk Management

- Stop-loss and take-profit levels are translated from pip distances using the detected pip size (with automatic 3/5 digit adjustment identical to the original code) and applied immediately after the position direction changes.
- Trailing logic follows the MetaTrader conditions: it becomes active once the price moves more than `TrailingStopPips + TrailingStepPips` away from the entry and only advances if the improvement exceeds the configured step.
- All exits are executed with `ClosePosition()` market orders because server-side stop/limit orders are not available in the high-level API.

## Notes

- The freeze/stop-level validation from the EA is not available in StockSharp, therefore the strategy only relies on the pip-size conversion and volume normalisation through `VolumeStep`, `VolumeMin` and `VolumeMax`.
- Risk-based sizing uses the current portfolio value and the entry price to approximate the MetaTrader margin check. This mirrors the general behaviour without depending on broker-specific margin calculators.
- Optional logging can be enabled through `EnableDetailedLog` for step-by-step diagnostics similar to `InpPrintLog` in MetaTrader.
