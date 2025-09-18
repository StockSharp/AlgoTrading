# Double MA Breakout Strategy

## Overview
The **Double MA Breakout Strategy** is a StockSharp port of the MetaTrader expert advisor `DoubleMA_Breakout`. The strategy monitors a fast and a slow moving average on finished candles. When the fast average moves above the slow one, a buy stop order is placed at a configurable breakout distance above the last close. When the fast average drops below the slow one, a sell stop is placed symmetrically below the market. Pending orders are cancelled and open positions are flattened when the crossover flips or the trading window closes.

The conversion keeps the core breakout logic, adds high-level order management, and exposes extensive configuration through `StrategyParam<T>` parameters. All comments in the code were rewritten in English as requested.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `FastMaPeriod` | 2 | Period of the fast moving average. |
| `SlowMaPeriod` | 5 | Period of the slow moving average. |
| `FastMaMode` | `Simple` | Moving average type for the fast line (SMA, EMA, SMMA, LWMA, LSMA). |
| `SlowMaMode` | `Simple` | Moving average type for the slow line. |
| `FastAppliedPrice` | `Close` | Applied price for the fast average (close, open, high, low, median, typical, weighted). |
| `SlowAppliedPrice` | `Close` | Applied price for the slow average. |
| `SignalShift` | 1 | Number of completed candles to look back when evaluating the crossover. `0` means the current candle. |
| `BreakoutDistancePoints` | 45 | Breakout distance in price steps used to place stop orders away from the latest close. |
| `UseTimeWindow` | `true` | Enables the start/stop hour filter. |
| `StartHour` | 11 | First hour (inclusive) when new trades are allowed. |
| `StopHour` | 16 | Last hour (inclusive) when trading is allowed. |
| `UseFridayCloseAll` | `true` | Close positions and cancel all pending orders once the Friday close time is reached. |
| `FridayCloseTime` | 21:30 | Time of day on Friday when the strategy performs a hard flat. |
| `UseFridayStopTrading` | `false` | Disable new entries after the configured Friday stop time while keeping existing positions. |
| `FridayStopTradingTime` | 19:00 | Time of day on Friday when new entries are blocked (if enabled). |
| `CandleType` | 1 hour | Candle data type used for both indicators and signals. |

## Trading Logic
1. Subscribe to finished candles defined by `CandleType` and compute two moving averages according to the selected modes and applied prices.
2. Maintain short rolling histories of indicator values so that the strategy can reference the candle selected by `SignalShift` without violating the "no GetValue" guideline.
3. **Bullish setup:** when the fast MA is above the slow MA on the signal candle, cancel any sell stop, close short positions, and place a buy stop order `BreakoutDistancePoints × PriceStep` above the last close if no orders or positions remain.
4. **Bearish setup:** when the fast MA is below the slow MA on the signal candle, cancel any buy stop, close long positions, and place a sell stop order the same distance below the market.
5. **Time management:** if the trading window is disabled or closed, all pending orders are cancelled. On Fridays, the optional stop-trading and hard-flat times are honoured before the weekend.
6. When a stop order is executed the opposing pending order is cancelled to avoid multiple simultaneous trades.

## Differences from the MetaTrader EA
- Money-management switches and custom trailing-stop schemes from the original script are not ported. StockSharp's `Volume` property defines trade size, and risk control can be added through standard protection modules.
- Error retries and low-level order loops are replaced with high-level StockSharp helpers (`BuyStop`, `SellStop`, `ClosePosition`, `CancelOrder`).
- Broker-specific concepts such as margin cut-offs or slippage corrections are omitted; these can be implemented separately if needed.
- LSMA mode uses StockSharp's `LinearRegression` indicator to approximate the least-squares moving average used in MetaTrader.

## Usage Notes
- Configure `Volume` before starting the strategy; by default StockSharp uses a single lot/contract.
- Combine the strategy with `StartProtection` (already invoked in code) to attach platform-level stop-loss or take-profit modules if required.
- For optimisation workflows, enable the desired parameters via the `.SetCanOptimize` settings provided in the constructor.
- Ensure the instrument has a valid `PriceStep`; otherwise the breakout distance falls back to `1` to avoid zero offsets.
