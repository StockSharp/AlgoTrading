# Cycle Lines Strategy

## Overview

Cycle Lines Strategy is the StockSharp port of the MetaTrader expert advisor "Cycle Lines". The original script combined chart drawing with manual trade buttons. This port focuses on the automated trading logic that accompanied those controls. The strategy trades MACD line crossovers, keeps risk tightly controlled via absolute stop-loss and take-profit limits, and supports break-even as well as trailing stop management.

When the MACD line crosses above its signal line, the strategy opens a long position. When the line crosses below the signal line, it opens a short position. Open trades are closed if the indicator flips to the opposite direction or any of the protective rules (stop-loss, take-profit, break-even or trailing stop) are triggered.

## Trading Rules

1. **Entry conditions**
   - **Long:** MACD crosses above the signal line on the selected candle series.
   - **Short:** MACD crosses below the signal line on the selected candle series.
   - Entries are only evaluated after the indicator is fully formed and the strategy is connected and allowed to trade.
2. **Exit conditions**
   - Opposite MACD crossover.
   - Stop-loss reached.
   - Take-profit reached.
   - Break-even protection level touched.
   - Trailing stop level touched.

## Parameters

| Name | Description | Default | Notes |
| ---- | ----------- | ------- | ----- |
| `Volume` | Order volume per trade. | `1` | Must be positive. |
| `MacdFastPeriod` | Fast EMA period inside the MACD calculation. | `12` | Optimizable. |
| `MacdSlowPeriod` | Slow EMA period inside MACD. | `26` | Optimizable. |
| `MacdSignalPeriod` | MACD signal-line period. | `9` | Optimizable. |
| `StopLoss` | Absolute price distance for the protective stop. | `0` | Disabled when set to `0`. |
| `TakeProfit` | Absolute price distance for the take-profit target. | `0` | Disabled when set to `0`. |
| `TrailingOffset` | Distance kept between the best favorable price and the trailing stop. | `0` | Disabled when set to `0`. |
| `BreakEvenTrigger` | Profit distance required before moving the stop to break-even. | `0` | Disabled when set to `0`. |
| `BreakEvenOffset` | Additional offset applied to the break-even level. | `0` | Allows locking some extra profit above/below entry. |
| `CandleType` | Candle series used for indicator calculations. | `15` minute time-frame candles | Accepts any `DataType` supported by StockSharp. |

## Position Management

- **Stop-loss / take-profit:** Both checks evaluate intrabar extremes (high/low) for finished candles, ensuring the exit respects the configured absolute distance from the entry price.
- **Break-even:** Once the price moves in favor by at least `BreakEvenTrigger`, the strategy arms a stop at `entry ± BreakEvenOffset`. Any retracement that touches this level closes the position.
- **Trailing stop:** For long trades the highest reached price is monitored. The stop level follows the high minus `TrailingOffset`. For short trades the logic mirrors the behavior around the lowest price.

## Usage Notes

- The strategy trades a single position at a time. Consecutive signals will not pyramid an existing position.
- Parameters are exposed as `StrategyParam<T>` objects and therefore support optimization inside StockSharp.
- To reproduce the original EA's default behavior, configure the MACD periods to `12 / 26 / 9` and set the risk parameters according to your desired pip values.

## Differences from the MQL Version

- Chart drawing features and manual BUY/SELL buttons were omitted because StockSharp handles visualization differently.
- All protective rules were rewritten to operate on candle data instead of MetaTrader tick events, which keeps them compatible with StockSharp's high-level API.
- Trailing and break-even logic is symmetric for long and short trades for clarity and robustness.
