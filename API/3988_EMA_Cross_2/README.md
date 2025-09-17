# EMA Cross 2 Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert advisor **"EMA_CROSS_2"** from the MQL repository. The original EA monitors two exponential moving averages (EMAs) and places market orders whenever the averages swap order. The conversion keeps the contrarian nature of the script – it buys when the long EMA moves above the short EMA and sells when the short EMA rises above the long EMA – while wrapping the logic into the high-level StockSharp strategy infrastructure.

The strategy operates on completed candles supplied by the configurable candle data type. Signals are evaluated on candle close to avoid repeated triggers inside the same bar. Risk management mimics the MetaTrader behaviour by using take-profit, stop-loss, and trailing stop distances expressed in broker points (price steps).

## Trading Logic
1. **Indicator calculation**
   - Calculate the short-period and long-period EMAs on every completed candle.
   - Skip the first indicator update, matching the original `first_time` flag that ignored the very first evaluation.
   - Afterwards, detect a direction change when the relative ordering between the long and short EMA flips.
2. **Signal interpretation**
   - When the long EMA moves above the short EMA the original EA opened a buy trade. The StockSharp port keeps this contrarian rule even though it behaves opposite to a classic crossover system.
   - When the short EMA closes above the long EMA the strategy opens a sell trade.
   - New positions are only allowed when no exposure is currently open, replicating the `OrdersTotal() < 1` condition.
3. **Order execution**
   - Trades are sent as market orders with a fixed configurable volume.
   - On entry the strategy records stop-loss and take-profit prices using the pip distance provided through parameters.
4. **Risk management**
   - On every finished candle the strategy checks whether price action touched the stored stop-loss or take-profit levels. Breaching either level closes the entire position with a market order.
   - A trailing stop (also defined in broker points) is applied once price moves favourably by more than the trailing distance. For long positions the protective stop is shifted upward; for short positions it trails price downward.
   - When the position becomes flat, the stored protective levels are cleared.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series used for indicator calculations and signal detection. | 15-minute time frame |
| `OrderVolume` | Volume of each market order in lots/contracts. | 2 |
| `TakeProfitPoints` | Distance to the take-profit level expressed in broker points (price steps). A value of `0` disables the take-profit. | 20 |
| `StopLossPoints` | Distance to the stop-loss level expressed in broker points. A value of `0` disables the stop-loss. | 30 |
| `TrailingStopPoints` | Distance used when trailing the open position. `0` disables the trailing stop. | 50 |
| `ShortEmaPeriod` | Length of the fast EMA. | 5 |
| `LongEmaPeriod` | Length of the slow EMA. | 60 |

## Implementation Notes
- The strategy uses `SubscribeCandles().Bind(shortEma, longEma, ProcessCandle)` to connect candle data with EMA indicators, following the preferred high-level API pattern.
- Indicator values are received as ready-to-use decimals in the binding callback, so no manual buffer indexing is necessary.
- Protective distances are converted from MetaTrader points to StockSharp prices by multiplying by the instrument `PriceStep`. If the instrument uses fractional pip pricing (3 or 5 decimals) the helper computes the pip size accordingly.
- Stop-loss, take-profit, and trailing behaviour are implemented internally with market exits because StockSharp does not expose the same `OrderModify` workflow as MetaTrader 4. The resulting trade management mirrors the original logic: levels are checked on every candle and exits occur immediately once breached.
- The first crossover evaluation is intentionally ignored to reproduce the `first_time` safeguard that prevented premature trades in the MQL script.

## Differences from the MetaTrader Version
- Money management: the original EA always traded the `Lots` parameter. The conversion exposes the same concept through `OrderVolume` and also assigns it to the strategy `Volume` property so designers and optimisers can reuse it.
- Order placement: MetaTrader applied stop-loss and take-profit directly within `OrderSend`. In StockSharp these levels are tracked by the strategy and closed with market orders when breached.
- Trailing stop precision: the EA moved stops using tick data (`Bid`/`Ask`). The port updates the trailing logic on candle close, which is the finest granularity available inside this sample project. The distance and activation rules remain identical.
- Error handling and logging were simplified; StockSharp logging provides detailed information through the standard strategy log.

## Usage Tips
- Align `CandleType` with the timeframe used during backtests of the original EA to maintain comparable indicator behaviour.
- When trading symbols quoted with fractional pips ensure that the configured point distances reflect the desired number of pips (for example, on EURUSD `10` points equal 1 pip).
- Set `OrderVolume` to the contract size expected by your execution venue. The strategy does not perform automatic volume scaling.
- Use the built-in optimisation toggles on each parameter to explore combinations of EMA periods and risk distances just like you would optimise inputs in MetaTrader.

## Files
- `CS/EmaCross2Strategy.cs` – StockSharp implementation of the trading logic.
- `README.md` – English documentation (this file).
- `README_cn.md` – Chinese translation.
- `README_ru.md` – Russian translation.
