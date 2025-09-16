# PROphet Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert advisor "PROphet". The original EA evaluates the recent trading ran
ge across four historical candles and uses those weighted ranges to trigger new trades. It keeps positions open only between the
European and U.S. sessions and trails the stop-loss whenever price moves a fixed distance in favour of the trade. The StockSharp
implementation keeps all of those mechanics while adapting them to the netting model used by StockSharp portfolios.

## Trading logic
- Subscribe to the configured timeframe (`CandleType`, default M5) and process only finished candles.
- Maintain the three most recent completed candles to reproduce the `High[i]` and `Low[i]` indexing used by the MQL version.
- Compute the long trigger `Qu(X1, X2, X3, X4)` and the short trigger `Qu(Y1, Y2, Y3, Y4)` on every bar. Each term multiplies a
weighted range (for example `|High[1] - Low[2]|`) by the corresponding weight minus one hundred, exactly as in the original code.
- Allow new entries only when the current hour falls between `TradeStartHour` and `TradeEndHour` (inclusive). This mimics the man
ual trading window from the MQL expert (10:00 through 18:00 by default).
- Use a single market order whose volume neutralises any opposite exposure before opening the new position. This mirrors the Mag
ic Number filters from the MetaTrader implementation.

## Risk management and trailing
- The strategy converts the MetaTrader point-based stop distances to price units via the instrument `PriceStep`. The defaults (`B
uyStopLossPoints = 68`, `SellStopLossPoints = 72`) match the MQL extern variables.
- Once the bid (for long trades) or the ask (for short trades) moves beyond the existing stop by `spread + 2 * stopDistance`, th
e trailing stop is advanced to `currentPrice ± stopDistance`, using live Level-1 data when available.
- Open trades are force-closed after `ExitHour`. The default value (18) reproduces the original behaviour of closing the position
s after 18:00 server time.
- Protective exits use market orders because StockSharp's high-level API does not automatically generate stop orders. This keeps
behaviour deterministic across brokers.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `AllowBuy` | Enables long trades. |
| `AllowSell` | Enables short trades. |
| `X1`, `X2`, `X3`, `X4` | Weights applied to the long-side range components inside the `Qu` formula. |
| `BuyStopLossPoints` | Stop-loss distance for long trades expressed in MetaTrader points. |
| `Y1`, `Y2`, `Y3`, `Y4` | Weights applied to the short-side range components inside the `Qu` formula. |
| `SellStopLossPoints` | Stop-loss distance for short trades expressed in MetaTrader points. |
| `TradeVolume` | Base volume (lots) used for new entries. Extra volume is added automatically to close opposite exposure. |
| `TradeStartHour` | First hour of the trading window (inclusive). |
| `TradeEndHour` | Last hour of the trading window (inclusive). |
| `ExitHour` | Hour after which all open trades are closed. |
| `CandleType` | Timeframe of the candles used for analysis. |

## Notes
- StockSharp portfolios are netting by default. When a new signal appears the strategy adds the volume required to flatten the ex
isting position before opening the new trade, which reproduces the single-position-per-direction design from the MetaTrader expe
rt.
- The MQL script used the symbol spread reported by `MarketInfo`. The port retrieves the spread from Level-1 data when available
and falls back to a single price step otherwise.
- Because the trailing stop is evaluated on the close of each finished candle, slippage may occur compared to the tick-level stop
updates performed by the original EA.
