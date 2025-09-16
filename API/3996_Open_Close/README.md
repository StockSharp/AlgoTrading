# Open Close Strategy (ID 3996)

## Overview
This strategy replicates the MetaTrader 4 expert `open_close.mq4`. It works on a single instrument and compares the open and close of the latest candle against the previous one. When no position is active, it fades strong one-bar moves (gap-and-reversal patterns). While in a trade, it closes the position either when the pattern reverses or when a floating-loss protection threshold is breached.

## Trading Logic
### Entry rules
- Trades only when the previous candle has been processed (the original `Volume[0] == 1` guard).
- Long entry: the current candle opens above the previous open **and** closes below the previous close. The strategy buys the configured volume at market.
- Short entry: the current candle opens below the previous open **and** closes above the previous close. The strategy sells short at market.

Only one position can be active at any time. New signals are ignored until the open position is closed.

### Exit rules
1. **Risk protection:** floating PnL is measured from the average entry price. If the unrealized loss exceeds `MaximumRisk × Portfolio.CurrentValue`, the strategy immediately closes the position. The original MQL version used `AccountMargin`, which is approximated here with the best available portfolio valuation.
2. **Pattern reversal:**
   - Long positions close when the next candle continues downward (`open < previous open` and `close < previous close`).
   - Short positions close when the next candle continues upward (`open > previous open` and `close > previous close`).

## Position Sizing
- The default order size is derived from `MaximumRisk`. The strategy multiplies the available account value by `MaximumRisk` and divides the result by `1000`, mimicking the MetaTrader calculation of `AccountFreeMargin * MaximumRisk / 1000`.
- If the account information is not available, the fallback `InitialVolume` parameter is used.
- After more than one consecutive losing trade, the lot size is reduced by `volume × losses / DecreaseFactor`, reproducing the MetaTrader loop over the history of closed trades.
- A minimum tradable volume of `0.1` lots is enforced before aligning the quantity to the instrument volume step and exchange limits.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `InitialVolume` | `decimal` | `0.1` | Fallback lot size used when equity information is not available. |
| `MaximumRisk` | `decimal` | `0.3` | Fraction of account value that controls both position sizing and the maximum tolerated floating loss. |
| `DecreaseFactor` | `decimal` | `100` | Reduction factor applied after more than one consecutive losing trade. |
| `CandleType` | `DataType` | `15m` time-frame | Candle series used to evaluate the pattern. |

## Implementation Notes
- The strategy subscribes to the selected candle series and processes **only finished candles**, matching the `Volume[0] > 1` condition in the original expert.
- Floating PnL is estimated from the strategy’s current position and the latest close price because StockSharp does not expose MetaTrader’s `AccountProfit` and `AccountMargin` metrics.
- Consecutive losses are tracked through filled trades, allowing `DecreaseFactor` to behave like the original loop over the trade history.
- Volume alignment respects `Security.VolumeStep`, `MinVolume`, and `MaxVolume` to stay compatible with exchange requirements.
- Charts are populated with candles and own trades when a chart area is available for visual debugging.

## Usage Tips
- Choose a candle interval that matches the one used in MetaTrader when calibrating the original expert.
- Adjust `MaximumRisk` and `DecreaseFactor` to tune the aggressiveness of the lot-sizing rule.
- Because the strategy is contrarian, it performs best on instruments that exhibit frequent single-bar overextensions and snap-back moves.
