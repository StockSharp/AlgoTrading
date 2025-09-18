# Heiken Ashi Engulf Strategy

## Overview
The strategy replicates the behaviour of the MetaTrader 5 experts **heiken ashi engulf ea buy mt5.mq5** and **heiken ashi engulf sell ea mt5.mq5** by combining both directions inside a single StockSharp high-level strategy. It reconstructs classic Heiken Ashi candles from the subscribed timeframe, waits for an engulfing pattern, confirms it with moving-average alignment and two RSI-based filters, and finally opens a market position with optional fixed stop-loss and take-profit distances expressed in MetaTrader pips.

The conversion keeps the original “buy” and “sell” configurations separate so that each side can be optimised independently. A direction selector allows traders to run only the bullish, only the bearish, or both playbooks at once.

## Trading Logic
### Heiken Ashi reconstruction
1. For every completed candle the strategy builds Heiken Ashi open, high, low and close values using the previous synthetic open and close (standard MT algorithm).
2. Two historical Heiken Ashi candles (`shift = 1` and `shift = 2`) are stored to emulate the `Shift` parameters from the MetaTrader code.

### Long setup
1. No open position is allowed (equivalent to the `NoOpenedOrders` block).
2. The latest Heiken Ashi candle must be bullish and the previous one bearish (`ChosenCandleType = 1`, `PreviousCandleType = 2`).
3. The most recent real candle must close above the high of the candle before it (`Close[1] > High[2]`), while that previous candle must be bearish (`Close[2] < Open[2]`).
4. The Heiken Ashi close of the newest candle must stay above the baseline moving average (`iMA` with parameters `BuyBaselineMethod/Period`).
5. The fast trend MA must be above the slow trend MA (`BuyFast` vs `BuySlow`).
6. Two RSI filters must keep their values inside the configured limits for the specified number of candles (the same logic as the `IndicatorWithinLimits` block, including the exception counter).
7. If all conditions pass the strategy buys the requested volume, converts the configured stop-loss and take-profit distances from pips to price units and sets protective orders through `SetStopLoss` / `SetTakeProfit`. An optional log message replicates the MetaTrader alert.

### Short setup
The short logic mirrors the long rules with opposite comparisons:
1. Flat position.
2. The latest Heiken Ashi candle is bearish and the previous one bullish.
3. The most recent real candle closes below the low of the candle before it (`Close[1] < Low[2]`), and that previous candle is bullish.
4. The Heiken Ashi close stays below the bearish baseline MA, while the fast MA remains below the slow MA.
5. Both RSI filters remain between their bounds, using their own shift/period/exception configuration.
6. A market sell order is placed and the stop-loss / take-profit distances for shorts are applied.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | H1 | Timeframe used for all indicators and signals. |
| `Direction` | Both | Which side of the engulfing playbook should be active (`BuyOnly`, `SellOnly`, `Both`). |
| `BuyVolume` | 0.01 | Lot size for long trades. |
| `BuyStopLossPips` | 50 | MetaTrader pips between entry and stop-loss for longs. `0` disables the fixed stop. |
| `BuyTakeProfitPips` | 50 | MetaTrader pips between entry and take-profit for longs. `0` disables the fixed target. |
| `BuyBaselinePeriod` / `BuyBaselineMethod` | 20 / Exponential | MA compared with the bullish Heiken Ashi candle (mirrors `inp1_Ro_*`). |
| `BuyFastPeriod` / `BuyFastMethod` | 20 / Exponential | Fast trend MA (`inp12_Lo_*`). |
| `BuySlowPeriod` / `BuySlowMethod` | 30 / Exponential | Slow trend MA (`inp12_Ro_*`). |
| `BuyPrimaryRsi*` | 14, shift 1, window 2, exceptions 0, limits [0;100] | First RSI filter (matches `inp13_*`). |
| `BuySecondaryRsi*` | 5, shift 2, window 3, exceptions 0, limits [0;100] | Second RSI filter (`inp14_*`). |
| `SellVolume` | 0.01 | Lot size for short trades. |
| `SellStopLossPips` | 50 | MetaTrader pips between entry and stop-loss for shorts. |
| `SellTakeProfitPips` | 50 | MetaTrader pips between entry and take-profit for shorts. |
| `SellBaselinePeriod` / `SellBaselineMethod` | 20 / Exponential | Baseline MA for bearish setups (`inp15_*`). |
| `SellFastPeriod` / `SellFastMethod` | 20 / Exponential | Fast trend MA (`inp26_Lo_*`). |
| `SellSlowPeriod` / `SellSlowMethod` | 30 / Exponential | Slow trend MA (`inp26_Ro_*`). |
| `SellPrimaryRsi*` | 14, shift 1, window 2, exceptions 0, limits [0;100] | First RSI filter for shorts (`inp27_*`). |
| `SellSecondaryRsi*` | 5, shift 2, window 3, exceptions 0, limits [0;100] | Second RSI filter for shorts (`inp28_*`). |
| `AlertTitle` | "Alert Message" | Text written to the log when a trade opens. |
| `SendNotification` | true | Enables the info log message that replaces MetaTrader pop-ups/notifications. |

## Risk Management
- Stop-loss and take-profit distances are converted from MetaTrader pips into price units. The conversion automatically scales the value according to the security tick size (3/5-digit quoting support included).
- When a new trade is executed the expected resulting position is passed to `SetStopLoss` / `SetTakeProfit`, mimicking the original virtual/real stop placement.
- No additional trailing logic was present in the source EA and therefore none is introduced.

## Notes
- The RSI filters use the same “window with exceptions” logic as the MetaTrader builder. If the number of available candles is insufficient the trade signal is ignored until enough history is collected.
- The Heiken Ashi values are cached per candle so that indicator shifts (`Shift + CandlesShift`) match the behaviour of the original `.mq5` files.
- Setting `Direction` to `BuyOnly` or `SellOnly` completely disables the opposite side without altering its parameters, which helps during optimisation.
