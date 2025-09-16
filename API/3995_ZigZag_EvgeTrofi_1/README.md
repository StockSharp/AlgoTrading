# ZigZag EvgeTrofi 1 Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

## Overview
ZigZag EvgeTrofi 1 reproduces the behavior of the original MetaTrader expert advisor that reacts to the latest ZigZag swing point. The strategy monitors every completed candle, identifies the freshest ZigZag pivot using the classic depth, deviation and backstep configuration, and enters the market if the pivot is still recent. A swing high triggers a long position, while a swing low opens a short position, matching the original EA signal map.

## Trading Logic
- Subscribe to the configured candle type and feed Highest/Lowest indicators whose length matches the ZigZag depth parameter. The pair of indicators emulate the native ZigZag swing detection without relying on custom buffers.
- When a candle closes, check whether its high touches the tracked maximum or its low touches the tracked minimum. Only switch to a new pivot if the required deviation in price steps is satisfied and the backstep distance (minimum bars between opposite pivots) is respected.
- Once a pivot is recorded, keep counting how many bars have passed. The urgency parameter defines how many bars after the pivot are still considered actionable. Signals older than this limit are ignored, preventing late entries.
- For a high pivot the strategy prepares to buy, and for a low pivot it prepares to sell. If an open position already matches the intended direction, the signal is marked as handled and no additional orders are submitted.
- If the account currently holds exposure in the opposite direction, the strategy sends a market order to flatten before opening a new trade. Afterwards it immediately submits a market order with the configured volume to establish the new position.
- Every action requires a fully formed indicator state, a finished candle and positive trading volume. The strategy checks connectivity and permissions using `IsFormedAndOnlineAndAllowTrading()` before interacting with the market, ensuring that orders are only sent under healthy trading conditions.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Depth` | ZigZag depth that defines the swing detection window. | 17 |
| `Deviation` | Minimum price movement in points required to confirm the same-type pivot. Converted to instrument price steps internally. | 7 |
| `Backstep` | Minimum number of bars that must pass before switching to an opposite pivot. | 5 |
| `Urgency` | Maximum number of bars after a pivot during which trades are allowed. | 2 |
| `Candle Type` | Candle data type (time frame or custom aggregation) used for calculations. | 5 minute time frame |
| `Volume` | Market order volume submitted on every entry. | 0.1 |

## Implementation Notes
- The Highest/Lowest indicators are bound via the high-level `SubscribeCandles().Bind()` API, so the strategy operates on final candles only and avoids manual buffering.
- The deviation parameter is transformed into an absolute price difference using the instrument price step. If the symbol lacks price step metadata, a value of 1 is used as a fallback, keeping the logic consistent across exchanges.
- A boolean guard prevents duplicate trades per pivot, matching the MetaTrader EA behavior that only acts once per swing.
- The built-in chart integration draws candles and executed trades automatically when charting is available, which helps to visually validate swing points and entries.
- Position management is symmetrical: any opposite exposure is flattened with a market order of equal volume before establishing the new trade, keeping the portfolio single-sided like the original expert advisor.
