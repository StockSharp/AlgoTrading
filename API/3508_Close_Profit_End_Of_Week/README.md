# Close Profit End Of Week Strategy

## Overview
The **Close Profit End Of Week Strategy** automates the MetaTrader script *Closeprofitendofweek.mq5*. The strategy supervises the assigned instrument and, on Fridays after a configurable cut-off time, exits every profitable position. The goal is to secure floating gains before the weekend gap risk appears.

## Original MQL Behaviour
The source Expert Advisor continuously polled positions through the timer handler. Whenever the server time matched Friday and the configured end time, it looped through all open positions on the traded symbol. Every position with positive profit was closed via a market order. Crypto symbols were explicitly excluded because they trade without weekend breaks.

## StockSharp Implementation
The C# port keeps the same protection logic while using StockSharp's high-level API:
- Subscribes to a configurable candle series only to receive regular time updates.
- Checks each finished candle and verifies that it represents a Friday whose closing time is after the user-defined cut-off.
- Accesses the connected portfolio to evaluate the net position for the strategy symbol.
- Issues a market order in the opposite direction for every profitable exposure that is still open.
- Skips the routine entirely when the instrument is classified as a crypto asset.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `StartTradeTime` | Beginning of the monitoring window (kept for parity with the MQL inputs). | `00:00` |
| `EndTradeTime` | Time of day on Friday after which profitable positions must be closed. | `20:00` |
| `CloseTradesAtEndTime` | Enables or disables the automatic closing routine. | `true` |
| `CandleType` | Data series used to track time (defaults to 1-minute candles). | `TimeFrameCandle(1m)` |

## Execution Flow
1. When the strategy starts it verifies whether the selected security belongs to the crypto-asset class. Crypto instruments are ignored to mirror the MetaTrader guard clause.
2. A candle subscription is created to receive regular callbacks once each candle is finished.
3. Every finished candle triggers the schedule checks. Only Fridays that closed after the cut-off time lead to further processing.
4. The strategy scans the connected portfolio, filters the position that corresponds to the configured security, and reads its floating profit.
5. If the floating profit is greater than zero, a market order is submitted in the opposite direction to fully close the exposure.
6. Duplicate exit orders are avoided by inspecting active orders before sending new ones.

## Usage Notes
- Attach the strategy to a non-crypto instrument together with the same portfolio that owns the open positions you want to supervise.
- The strategy does not open any new trades; it only manages existing positions.
- The `StartTradeTime` parameter exists for configuration parity and future extensions but is not referenced by the current logic.
- For multi-symbol portfolios, run one instance per instrument to replicate the single-symbol scope of the MetaTrader script.

## Limitations
- Profit detection relies on the broker portfolio reporting floating PnL. If the portfolio does not update in real time the closing command may be delayed.
- Only positions for the configured strategy symbol are closed. Exposures held on other symbols remain untouched.
- The check is executed on candle close events. Select an appropriately short timeframe if you need a tighter schedule.
