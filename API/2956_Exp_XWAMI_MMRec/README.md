# Exp XWAMI MMRec (ID 2956)

## Summary

The strategy replicates the MetaTrader expert advisor **Exp_XWAMI_MMRec**, combining the custom XWAMI momentum indicator with a money-management "recounter". Momentum is measured as the difference between the current price and the price `Period` bars ago. That difference is fed through four configurable smoothing stages; the third and fourth stages form the `Up` and `Down` buffers of the original indicator. Crossings between the two buffers drive position reversals.

Each stage can emulate several smoothing algorithms: simple/ exponential/ smoothed/ linear weighted moving averages, Jurik JJMA/JurX, Tillson T3, VIDYA (approximated with EMA), and Kaufman's AMA. The strategy works with a single aggregated position and supports both long and short trades. Risk is reduced after consecutive losses by comparing recent trade results against the `BuyTotalTrigger`/`SellTotalTrigger` windows and counting losses relative to `BuyLossTrigger`/`SellLossTrigger`.

Protective stops follow the MetaTrader implementation: `StopLossPoints` and `TakeProfitPoints` are measured in symbol points (`Security.PriceStep`). When a stop or target is touched inside the signal timeframe, the position is closed immediately and the trade result enters the money-management history.

## Parameters

| StockSharp property | Default | Original input | Description |
| --- | --- | --- | --- |
| `CandleType` | H1 time frame | `InpInd_Timeframe` | Timeframe used to build candles for the indicator. |
| `Period` | 1 | `iPeriod` | Distance (in bars) between the current price and the comparison price inside the momentum calculation. |
| `Method1` / `Length1` / `Phase1` | `T3`, `4`, `15` | `XMethod1`, `XLength1`, `XPhase1` | Smoothing method, length, and phase for stage 1. Phase is only used by Jurik/JurX/T3. |
| `Method2` / `Length2` / `Phase2` | `Jjma`, `13`, `15` | `XMethod2`, `XLength2`, `XPhase2` | Settings for the second smoothing stage. |
| `Method3` / `Length3` / `Phase3` | `Jjma`, `13`, `15` | `XMethod3`, `XLength3`, `XPhase3` | Settings for the third smoothing stage (indicator `Up` buffer). |
| `Method4` / `Length4` / `Phase4` | `Jjma`, `4`, `15` | `XMethod4`, `XLength4`, `XPhase4` | Settings for the fourth smoothing stage (indicator `Down` buffer). |
| `AppliedPrice` | `Close` | `IPC` | Price source forwarded into the momentum calculation. All MetaTrader price options are reproduced, including both TrendFollow flavours and the Demark price. |
| `SignalBar` | 1 | `SignalBar` | Index of the historical candle used to evaluate crossings (`0` = most recent finished bar). |
| `AllowBuyOpen` / `AllowSellOpen` | `true` | `BuyPosOpen`, `SellPosOpen` | Enables long or short entries respectively. |
| `AllowBuyClose` / `AllowSellClose` | `true` | `BuyPosClose`, `SellPosClose` | Enables forced exits when the opposite signal appears. |
| `NormalVolume` | `0.1` | `MM` | Default lot/volume size used after profitable or neutral series. |
| `ReducedVolume` | `0.01` | `SmallMM_` | Reduced lot applied after too many losses. |
| `BuyTotalTrigger` / `BuyLossTrigger` | `5` / `3` | `BuyTotalMMTriger`, `BuyLossMMTriger` | Number of recent long trades inspected and maximum losses inside that window before reducing the long volume. |
| `SellTotalTrigger` / `SellLossTrigger` | `5` / `3` | `SellTotalMMTriger`, `SellLossMMTriger` | Same logic for short positions. |
| `StopLossPoints` | `1000` | `StopLoss_` | Stop-loss distance in points. |
| `TakeProfitPoints` | `2000` | `TakeProfit_` | Take-profit distance in points. |

## Behaviour

1. Subscribe to the requested candle series and evaluate only finished candles.
2. Compute the price difference (`AppliedPrice` now vs. `Period` bars ago). When enough history is available, feed the difference through the four smoothing stages.
3. Store the third (`Up`) and fourth (`Down`) stage outputs. When `Up` and `Down` on `SignalBar + 1` (the previous bar) cross, the strategy flips bias. If `Up > Down`, short positions are closed and a long position is opened if `Up <= Down` on the signal bar. The opposite logic handles bearish signals.
4. Position size is selected by the recounter: the last `BuyTotalTrigger` (or `SellTotalTrigger`) trade profits are inspected. If at least `BuyLossTrigger` (or `SellLossTrigger`) of them are negative, the next trade uses `ReducedVolume`; otherwise `NormalVolume` is used.
5. When a long position exists, stop-loss and take-profit distances are converted from points to price by multiplying with `Security.PriceStep`. On breach the position is closed at the stop/target price and the trade is recorded for the money-management module. Short trades follow the symmetrical rules.

## Differences from the MetaTrader version

- StockSharp aggregates positions, therefore `BuyMagic`/`SellMagic`, the MetaTrader global-variable bookkeeping, and the `MarginMode` option are unnecessary and were omitted.
- Tillson T3 is implemented explicitly; Jurik JJMA and JurX both map to `JurikMovingAverage` with the provided phase. VIDYA and ParMA are approximated with an exponential moving average because StockSharp lacks native equivalents.
- Orders are executed with `BuyMarket`/`SellMarket` and stops/targets are enforced by monitoring candle highs/lows rather than by native MT5 stop orders.
- Deviation/slippage input is not required on StockSharp execution models and was removed.

## Usage notes

1. Choose the instrument and set `CandleType` to the timeframe used by the original expert.
2. Configure the smoothing methods and lengths to match the MetaTrader indicator settings.
3. Adjust `NormalVolume`, `ReducedVolume`, and the trigger thresholds to align with the desired risk policy.
4. Attach the strategy to a portfolio and start it; trading is fully automated and reverses on every indicator crossing.

For further customisation you can edit the smoothing mappings inside `ExpXwamiMmRecStrategy.CreateFilter` to plug in alternative StockSharp indicators.
