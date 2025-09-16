# Iin MA Signal Strategy

## Overview
The strategy reproduces the behaviour of the classic **Iin MA Signal** MQL5 expert advisor. It watches for a crossover between a fast and a slow moving average and reacts on the bar defined by the `SignalBar` parameter, just like the original template that polled the indicator buffers. Bullish crosses open long positions and optionally close existing shorts, while bearish crosses open shorts and optionally close longs. Stops and targets can be attached automatically through StockSharp position protection.

## Trading logic
1. Subscribe to a single candle series specified by `CandleType` (default: 1-hour candles).
2. Build two moving averages using the types and lengths defined by `FastMaType`/`FastPeriod` and `SlowMaType`/`SlowPeriod`. SMA, EMA, SMMA (RMA) and LWMA are supported to cover the combinations available in the MQL source.
3. Store a rolling window of moving-average values so the crossover can be evaluated on the candle index given by `SignalBar`. This mimics the `CopyBuffer` requests from the original expert.
4. Detect a bullish cross when the fast MA was below the slow MA on the previous bar of the window and rises above it on the signal bar while the previous trend was not already bullish. A bearish cross is detected in the symmetric way.
5. Update the internal trend flag after each confirmed cross to avoid duplicate entries and to replicate the `trend` guard variable from the MQL indicator.
6. When trading is allowed (`IsFormedAndOnlineAndAllowTrading()` returns true), send the market orders defined by the entry/exit flags.

## Entry rules
- **Long entry**: triggered on a bullish cross if `AllowLongEntries` is enabled and the current position is flat or short. Any open short can be closed first when `CloseShortOnSignal` is true.
- **Short entry**: triggered on a bearish cross if `AllowShortEntries` is enabled and the current position is flat or long. Any open long can be closed first when `CloseLongOnSignal` is true.

## Exit rules
- Opposite signals can close positions according to the `CloseLongOnSignal` and `CloseShortOnSignal` switches.
- Optional protective exit levels use absolute price distances: `StopLossPoints` and `TakeProfitPoints`. When either value is greater than zero, the strategy calls `StartProtection` to arm the stop-loss and/or take-profit using market orders.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `CandleType` | Data type describing the candle series used for calculations. | 1-hour time frame |
| `FastPeriod` | Period of the fast moving average. | 10 |
| `FastMaType` | Type of the fast moving average (`Sma`, `Ema`, `Smma`, `Lwma`). | `Ema` |
| `SlowPeriod` | Period of the slow moving average. | 22 |
| `SlowMaType` | Type of the slow moving average (`Sma`, `Ema`, `Smma`, `Lwma`). | `Sma` |
| `SignalBar` | Number of finished bars back that must contain the crossover (1 reproduces the MQL default). | 1 |
| `AllowLongEntries` | Enable or disable long entries. | `true` |
| `AllowShortEntries` | Enable or disable short entries. | `true` |
| `CloseLongOnSignal` | Close long positions when a bearish signal appears. | `true` |
| `CloseShortOnSignal` | Close short positions when a bullish signal appears. | `true` |
| `StopLossPoints` | Absolute stop-loss distance in price units (0 disables). | 1000 |
| `TakeProfitPoints` | Absolute take-profit distance in price units (0 disables). | 2000 |

## Implementation notes
- High-level StockSharp APIs are used throughout: `SubscribeCandles` requests market data and `Bind` streams the MA values directly into the strategy without manual history handling.
- The moving-average factory (`CreateMa`) maps the enum values to StockSharp indicators, avoiding custom calculations.
- A compact in-memory buffer keeps only `SignalBar + 2` samples, which is enough to evaluate the crossover on the requested bar and the preceding one.
- Protective orders are optional and are initialised only if non-zero distances are configured, replicating the optional MM module from the MQL version.
- All comments in the code are written in English per repository rules.

## Usage
1. Build the solution (`dotnet build AlgoTrading.sln`) to compile the new strategy.
2. Instantiate `IinMaSignalStrategy` in your application, configure the desired parameters, and assign a connector/security/portfolio before starting it.
3. Optionally attach the strategy to a chart to visualise the fast and slow moving averages together with executed trades.
4. Optimise the MA periods, the signal bar and the risk settings to adapt the template to different markets.

## Differences from the original MQL expert
- The StockSharp version uses high-level subscription and indicator binding instead of manual buffer queries.
- Money-management helpers from `TradeAlgorithms.mqh` are replaced by `StartProtection`, which offers equivalent stop and target automation.
- Position management is flat by default: the strategy avoids hedging by not opening a new position while the opposite side is still active unless the closing flag is disabled.
- Chart rendering leverages StockSharp helper methods and does not attempt to replicate the original arrow buffers.
