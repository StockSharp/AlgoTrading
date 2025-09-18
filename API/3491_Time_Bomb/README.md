# Time Bomb Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Time Bomb replicates the MetaTrader expert advisor that fires a single order whenever price explodes in one direction within a
short, configurable window. The strategy watches real-time best bid/ask quotes and measures the number of pips covered between
the last reference price and the newest quote. If the required distance is travelled quickly enough it opens a market order in
the direction of the breakout and immediately arms hidden stop-loss and take-profit levels expressed in pips.

The implementation acts only when no position is currently open, mirroring the original block logic that prevented overlapping
trades. Price references are reset either once a signal is triggered or when the observation window expires, so each burst of
volatility produces at most a single trade per side. Stop-loss and take-profit levels are maintained internally and enforced by
the strategy itself because StockSharp does not automatically place protective orders for market executions.

## Details

- **Entry Criteria**:
  - **Long**: The best ask rises by at least `BuyPipsInTime` pips compared with the stored reference price and the move finishes
    within `BuyTimeToWait` seconds. A buy order with size `BuyVolume` is sent once the condition is met.
  - **Short**: The best bid falls by at least `SellPipsInTime` pips compared with the stored reference price and the move finishes
    within `SellTimeToWait` seconds. A sell order with size `SellVolume` is sent once the condition is met.
- **Long/Short**: Both directions are supported but only one position can exist at a time.
- **Exit Criteria**:
  - **Long**: Position closes when the best bid touches the calculated stop-loss or take-profit price.
  - **Short**: Position closes when the best ask hits the calculated stop-loss or the best bid reaches the take-profit level.
- **Stops**: Hidden protective stops are handled by the strategy. Distances are defined in pips and translated into prices using
  the current symbol step size.
- **Default Values**:
  - `SellPipsInTime` = 5 pips, `SellTimeToWait` = 10 seconds, `SellVolume` = 0.01 lots.
  - `SellStopLossPips` = 20 pips, `SellTakeProfitPips` = 20 pips.
  - `BuyPipsInTime` = 5 pips, `BuyTimeToWait` = 10 seconds, `BuyVolume` = 0.01 lots.
  - `BuyStopLossPips` = 20 pips, `BuyTakeProfitPips` = 20 pips.
- **Filters**:
  - Category: Breakout / momentum.
  - Direction: Symmetric (long and short).
  - Indicators: Raw price movement only, no oscillators.
  - Stops: Yes (fixed pip distances per side).
  - Complexity: Low—single breakout detector with simple state tracking.
  - Timeframe: Intra-day, reacts to tick-level impulses once per second.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Depends on configured pip distances; defaults correspond to medium risk on major FX pairs.

## Inputs

| Name | Description |
| --- | --- |
| `SellPipsInTime` | Minimum downward distance in pips that must be covered before opening a short position. |
| `SellTimeToWait` | Seconds allowed for the downward move to complete. |
| `SellVolume` | Trade volume for sell signals. |
| `SellStopLossPips` | Stop-loss distance for short positions, expressed in pips. |
| `SellTakeProfitPips` | Take-profit distance for short positions, expressed in pips. |
| `BuyPipsInTime` | Minimum upward distance in pips that must be covered before opening a long position. |
| `BuyTimeToWait` | Seconds allowed for the upward move to complete. |
| `BuyVolume` | Trade volume for buy signals. |
| `BuyStopLossPips` | Stop-loss distance for long positions, expressed in pips. |
| `BuyTakeProfitPips` | Take-profit distance for long positions, expressed in pips. |

## Notes

- The strategy relies on best bid/ask updates; ensure the data feed supplies accurate level-one quotes.
- Setting any pip distance or time window to zero disables the corresponding signal because the reference price resets instead of
  generating trades.
- Because protective levels are managed internally, unexpected disconnections can leave positions without hard stops. Consider
  combining the strategy with external risk controls when running live.
