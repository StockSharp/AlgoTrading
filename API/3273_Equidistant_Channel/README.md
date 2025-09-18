# Equidistant Channel Strategy

## Overview
The **Equidistant Channel Strategy** ports the original "Equidistant Channel" MQL4 expert advisor to the StockSharp high-level API. The strategy analyses MACD line crossovers and manages existing positions through Bollinger Band touches, breakeven logic, and money-based trailing targets.

When the MACD line crosses above its signal the strategy opens long positions, and when it crosses below the signal it opens short positions. While a trade is active the strategy watches for exits when price reaches Bollinger Bands, when floating profit hits configurable monetary or percentage targets, or when a trailing drawdown threshold is violated. A breakeven mode mirrors the MetaTrader implementation by moving the protective stop once profit exceeds a configurable number of price steps.

## Indicators
- **MACD (12, 26, 9)** — generates entry signals on crossovers between the MACD line and its signal line.
- **Bollinger Bands (20, 2)** — provide exit levels whenever the candle close hits the upper or lower band.

## Position Management
- Optional stop loss, take profit, and trailing stop distances expressed in price points via `StartProtection`.
- Money-based take profit and trailing logic that track floating profit using instrument price/step size metadata.
- Percentage-based take profit calculated from the starting portfolio value.
- Breakeven mode that pushes the stop to entry plus an offset once profit reaches a defined trigger.

## Parameters
| Group | Name | Default | Description |
| --- | --- | --- | --- |
| Trading | Volume | 1 | Order volume for new entries. |
| General | Candle Type | 5 minute | Candle series used for calculations. |
| Indicators | MACD Fast | 12 | Fast EMA length for MACD. |
| Indicators | MACD Slow | 26 | Slow EMA length for MACD. |
| Indicators | MACD Signal | 9 | Signal line length for MACD. |
| Indicators | BB Period | 20 | Bollinger Bands lookback period. |
| Indicators | BB Deviation | 2 | Bollinger Bands width in standard deviations. |
| Risk | Stop Loss | 20 | Stop loss distance in price points. |
| Risk | Take Profit | 50 | Take profit distance in price points. |
| Risk | Trailing Stop | 40 | Trailing stop distance in price points. |
| Risk | Use TP (Money) | false | Close when floating profit reaches an absolute money target. |
| Risk | TP Money | 10 | Absolute take profit value in account currency. |
| Risk | Use TP (%) | false | Close when floating profit reaches a percent of initial capital. |
| Risk | TP Percent | 10 | Percent of initial capital for the percentage take profit. |
| Risk | Enable Trailing | true | Enables trailing logic on floating profit. |
| Risk | Trail Activate | 40 | Profit level (currency) that arms the trailing logic. |
| Risk | Trail Step | 10 | Maximum allowed drawdown from the profit peak (currency). |
| Risk | Use BB Stop | true | Enable exits when price touches Bollinger Bands. |
| Risk | Use Breakeven | true | Enable the breakeven behaviour. |
| Risk | Breakeven Trigger | 10 | Profit (price steps) required to arm the breakeven stop. |
| Risk | Breakeven Offset | 5 | Offset (price steps) applied to the breakeven level. |

## Notes
- The strategy works with a single instrument that provides valid `PriceStep` and `StepPrice` metadata so that monetary calculations are accurate.
- The trailing profit module follows the MetaTrader behaviour: once floating profit exceeds the activation threshold the strategy records the running maximum and closes the trade when the drawdown exceeds the configured trailing step.
- Breakeven logic mirrors the original EA by using price-step based triggers and offsets.
- All comments inside the strategy code are written in English as required by the project guidelines.
