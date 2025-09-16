# Close All Positions By Time Strategy

## Overview
This strategy replicates the MetaTrader expert advisor *Exp_CloseAllPositionsByTime*. It constantly watches the trading clock and, once the configured stop time is reached, it flattens every open position managed by the strategy. The implementation is designed for StockSharp's high level API and uses a candle subscription solely as a heartbeat to evaluate the current time.

## Trading Logic
1. Subscribe to a configurable candle series (1 minute by default) to receive time updates.
2. As soon as a finished candle has an opening time greater than or equal to the target stop time, mark the strategy as stopped.
3. While in the stopped state, iterate over all positions owned by the strategy (main security plus any child strategies) and send market orders to neutralize them.
4. Continue issuing exit orders on subsequent candles until the entire book is flat, then clear the internal flag so the logic does not keep resubmitting orders.

The strategy never opens new trades; it only closes existing exposure, making it useful as an account-wide kill switch for discretionary or automated systems that may leave residual positions after the market closes.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Candle series used to drive the time checks. |
| `StopTime` | `DateTimeOffset` | `2030-01-01 23:59:00 +00:00` | Moment after which every open position should be closed. |

### Behavioral Notes
- Only completed candles trigger the exit logic to avoid premature closing during partially formed bars.
- The stop time comparison relies on the candle opening time. For backtests, ensure the candle timestamps match the desired exchange time zone.
- The strategy inspects `Positions` to cover instruments traded by nested child strategies. Each instrument is closed independently with a market order on the opposite side.

## Usage Tips
- Attach the strategy to a portfolio that might hold multiple symbols; it will automatically discover them through `Positions`.
- For intraday kill switches, set `StopTime` to the desired local session end (for example, `2024-05-01 16:00` in the exchange time zone).
- Increase the candle interval only if you are comfortable with lower time resolution. A shorter interval (1 minute) produces faster reaction after the deadline.
- Combine this module with other trading strategies by running it as a standalone strategy that shares the same portfolio.

## Conversion Notes
- The original MQL5 expert closed positions by looping over `PositionsTotal()` and sending opposite market orders with a slippage tolerance. StockSharp abstracts order creation, so the strategy calls `BuyMarket`/`SellMarket` to flatten positions without manually building requests.
- The StockSharp version keeps the `Stop` flag semantics: once the stop time is breached the strategy enters a closing state and remains there until the portfolio is flat.
- No attempt is made to close pending orders; only net positions are flattened, mimicking the source script.

## Requirements
- StockSharp environment configured with the target portfolio and securities.
- Incoming candles for the selected timeframe to trigger the time checks.

## Limitations
- If liquidity is insufficient, some exit orders may remain partially filled; the strategy will continue trying on the next finished candle.
- The component does not cancel outstanding limit orders; only net positions are handled. Cancel orders manually if needed.
