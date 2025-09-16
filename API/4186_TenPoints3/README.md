# Ten Points 3 Strategy

## Summary
- Converts the MetaTrader 4 expert advisor **10p3v004 ("10points 3")** into the StockSharp high level strategy framework.
- Recreates the MACD slope based grid entry logic together with martingale scaling, trailing protection and equity-based exits.
- Provides extensive documentation of every parameter so the behaviour of the original EA can be reproduced or safely tuned.

## Trading Logic
1. **Signal detection.** On every completed candle of the configured timeframe the strategy calculates a MACD with user-defined fast, slow and signal lengths. When the main MACD value rises compared to the previous bar the system prepares a long grid; when it falls a short grid is prepared. The `ReverseSignals` flag flips this interpretation.
2. **Grid entries.** Only one directional grid may be active at a time. The first order is placed immediately after a signal. Additional orders are added if:
   - The active grid direction matches the current signal, and
   - Price has moved by at least `GridSpacingPoints * PriceStep` from the most recent fill in the favourable averaging direction, and
   - The number of open grid trades has not reached `MaxTrades`.
   The order size is multiplied by `2^n` for small grids (up to 12 entries) or `1.5^n` for larger grids, reproducing the martingale logic from the source code. The final size is rounded to the instrument volume step and bounded by both the security limits and the `MaxVolumeCap` safety ceiling.
3. **Money management.** When `UseMoneyManagement` is enabled the base lot size is derived from the current portfolio value and `RiskPerTenThousand`. The original EA used separate rules for standard and mini accounts; this conversion keeps the same behaviour via the `IsStandardAccount` parameter. If the setting is disabled the fixed `BaseVolume` is used.
4. **Exit rules.**
   - Optional **initial stop** closes the whole grid if the aggregated position moves against it by `InitialStopPoints`.
   - Optional **take profit** closes the grid once price reaches `TakeProfitPoints` in favour of the net position.
   - Optional **trailing stop** starts following price after it moves by `(TrailingStopPoints + GridSpacingPoints)` from the average entry price and keeps a trailing buffer of `TrailingStopPoints`.
   - Optional **equity protection** monitors unrealised profit measured in points times volume. When `OrdersToProtect` or more positions are open and the profit reaches `SecureProfit`, the strategy exits immediately.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Primary timeframe used for MACD calculations and order processing. | 30-minute candles |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD configuration identical to the MT4 indicator (14/26/9 by default). | 14 / 26 / 9 |
| `BaseVolume` | Initial lot size used when no grid position exists and money management is disabled. | 0.01 |
| `GridSpacingPoints` | Minimum distance between consecutive grid entries, expressed in price steps. | 15 |
| `TakeProfitPoints` | Distance from the average entry to trigger a full take profit. Set to `0` to disable. | 40 |
| `InitialStopPoints` | Maximum adverse distance tolerated before flattening the grid. Set to `0` to disable. | 0 |
| `TrailingStopPoints` | Size of the trailing buffer. The trail activates after the price has advanced by `GridSpacingPoints + TrailingStopPoints`. | 20 |
| `MaxTrades` | Maximum number of averaging orders per direction. | 9 |
| `OrdersToProtect` | Minimum number of open trades required before the equity protection check is evaluated. | 3 |
| `SecureProfit` | Unrealised profit target (points × volume) which triggers the equity protection exit. | 8 |
| `AccountProtectionEnabled` | Enables or disables the equity protection block. | `true` |
| `ReverseSignals` | Inverts MACD slope interpretation (useful for mirrored testing). | `false` |
| `UseMoneyManagement` | Enables dynamic volume calculation using `RiskPerTenThousand`. | `false` |
| `RiskPerTenThousand` | Risk amount per 10,000 units of balance used when money management is active. | 12 |
| `IsStandardAccount` | Replicates the original lot rounding rules (`true` = standard lots, `false` = mini lots). | `true` |
| `MaxVolumeCap` | Hard cap applied after martingale scaling to keep the position size under control. | 100 |

## Conversion Notes
- The MQL expert maintained separate ticket-level stops. In StockSharp the grid is managed as a single aggregated position. Trailing and protective levels are therefore recalculated from the volume-weighted average entry price.
- The EA relied on broker tick value to convert profits to currency. Here the equity protection threshold is measured in points multiplied by volume, mirroring the pip-based comparison of the source.
- `AccountFreeMarginCheck` and other account-specific MT4 validations do not have a direct StockSharp equivalent. The strategy instead respects instrument volume bounds and the optional `MaxVolumeCap`.
- Order comments, magic numbers and graphical annotations from MT4 are not reproduced because they have no StockSharp counterpart.

## Usage
1. Add the strategy to your project, set `Security` and `Portfolio` as usual for StockSharp strategies.
2. Adjust `CandleType` to match the timeframe that should be analysed (the MT4 version worked on the current chart timeframe).
3. Tune risk parameters: either keep the fixed `BaseVolume` or enable `UseMoneyManagement` with appropriate `RiskPerTenThousand` and `IsStandardAccount` options.
4. Decide which protective layers to enable (initial stop, take profit, trailing stop, equity protection) and set the thresholds to suit the instrument volatility.
5. Start the strategy; the built-in chart helpers will display candles, MACD values and executed trades.

## Further Development Ideas
- Integrate adaptive spacing logic (for example using ATR) instead of the fixed `GridSpacingPoints`.
- Expose separate trailing parameters for long and short grids or allow asymmetric grids.
- Combine the MACD slope with trend filters (moving averages, higher timeframe confirmation) to reduce the number of counter-trend grids.

> **Note:** No Python implementation is provided for this strategy, matching the request and the current project structure.
