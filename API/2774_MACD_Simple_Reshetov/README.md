# MACD Simple Reshetov Strategy

## Overview
This strategy reproduces the behavior of Yury Reshetov's "MACDSimple" MetaTrader expert advisor inside the StockSharp framework. It works with a single security and evaluates classic MACD signals that are modified by two offset parameters. The algorithm processes completed candles only, ensuring that all trading decisions are made on confirmed data and avoiding intrabar noise.

## Indicators and Calculations
- **MACD (Moving Average Convergence Divergence)** – the MACD line and signal line are calculated with custom periods:
  - Fast EMA period = `SignalPeriod + DF`
  - Slow EMA period = `SignalPeriod + DS + DF`
  - Signal line period = `SignalPeriod`
The offsets `DF` and `DS` follow the original expert inputs and allow the trader to stretch or compress the MACD components while keeping their relationship intact.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `Volume` | Order size used for every market entry. | 2 |
| `DF` | Offset added to the fast MACD EMA length. Must be zero or positive. | 1 |
| `DS` | Additional offset applied to the slow MACD EMA length. Must be zero or positive. | 2 |
| `SignalPeriod` | Base period from which the fast and slow EMA lengths are derived. | 10 |
| `CandleType` | Timeframe of the candles used for analysis and trading. | 30-minute time frame |

## Trading Logic
### Position Handling
1. On every finished candle the strategy updates the MACD indicator and ignores the bar if the indicator is not fully formed yet.
2. If a **long** position is open and the MACD line drops below zero, the strategy closes the entire long position at market price.
3. If a **short** position is open and the MACD line rises above zero, the strategy closes the entire short position at market price.
4. After closing a position on a given bar the algorithm stops processing that bar, mirroring the behavior of the original expert advisor.

### Entry Rules
1. The algorithm verifies that both the MACD line and the signal line share the same sign (both positive or both negative). Mixed signs produce no trades.
2. When both lines are **positive**, a long position is opened if the MACD line is above the signal line.
3. When both lines are **negative**, a short position is opened if the MACD line is below the signal line.
4. Market orders are sized with the configured `Volume` parameter. Only one position can exist at a time.

### Exit Rules
- Exits are driven solely by the MACD line crossing the zero level against the open position, as described in the position handling section. No partial exits, stop losses, or take profits are implemented by default.

## Additional Notes
- The strategy trades only when `IsFormedAndOnlineAndAllowTrading()` is satisfied, ensuring that live data is available and trading is enabled before entering new positions.
- No automatic risk management is built in. Users can add custom protections such as `StartProtection()` or combine the strategy with portfolio-level risk controls if desired.
- Because the MACD parameters are derived from a single base period plus offsets, adjusting `SignalPeriod`, `DF`, or `DS` affects all components simultaneously, preserving the relative spacing intended by the original expert advisor.

## Implementation Details
- The indicator binding uses StockSharp's high-level `SubscribeCandles().Bind()` API, keeping the implementation concise and event-driven.
- The conversion follows the rule set described in `AGENTS.md`: tabs are used for indentation, indicator values are consumed directly from the binding callback, and trading functions `BuyMarket`/`SellMarket` manage entries and exits.
- The strategy structure is ready for extension (for example, adding filters or risk logic) while staying faithful to the original MetaTrader expert logic.
