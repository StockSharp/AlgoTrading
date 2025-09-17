# Trend Capture Legacy Strategy

**Trend Capture Legacy Strategy** ports the MetaTrader expert `TrendCapture.mq4` to the high-level StockSharp API. The C# version keeps the original rule set based on Parabolic SAR direction, a low ADX filter and simple break-even money management.

## Core ideas
- Process finished candles of the selected timeframe and feed them to Parabolic SAR (`0.02/0.2`) and Average Directional Index (`14`).
- Enter only when ADX is below the `AdxThreshold`, signalling a calm market where SAR reversals are more reliable.
- Remember the direction and outcome of the last closed trade: repeat the same side after a winner, flip to the opposite side after a loser.
- Apply fixed-distance stop-loss and take-profit levels (configured in price points) and move the stop to break-even once the trade gains `BreakEvenGuard` points.
- Size the order volume from the available portfolio value and `MaximumRisk`; fall back to the strategy `Volume` when portfolio information is unavailable.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `SarStep` | 0.02 | Initial Parabolic SAR acceleration step. |
| `SarMax` | 0.2 | Maximum Parabolic SAR acceleration. |
| `AdxPeriod` | 14 | ADX averaging period. |
| `AdxThreshold` | 20 | Maximum ADX value that still allows a fresh entry. |
| `TakeProfitPoints` | 180 | Take-profit distance in price points. |
| `StopLossPoints` | 50 | Stop-loss distance in price points. |
| `BreakEvenGuard` | 5 | Profit buffer (in points) required before moving the stop to entry. |
| `MaximumRisk` | 0.03 | Fraction of free margin used for position sizing. |
| `CandleType` | 1 hour candles | Timeframe for indicator calculations and trading signals. |

## Order management
- Long entries require the close price above SAR with low ADX; shorts require the close price below SAR with the same ADX filter.
- Stop-loss and take-profit levels are recalculated on every entry and evaluated on each completed candle.
- Break-even activation simply shifts the stop to the entry price. If no stop-loss is configured (zero or negative distance), the guard is ignored.

## Indicators
- `ParabolicSar` for directional bias.
- `AverageDirectionalIndex` for the strength filter (only the main ADX line is used).

## Notes
- The strategy uses `BindEx` to avoid direct buffer access, following the project guidelines.
- Portfolio-based volume calculation respects board constraints (`LotStep`, `MinVolume`, `MaxVolume`).
- Trade history needed for direction bias is gathered via `OnNewMyTrade`, so partial fills remain supported.
