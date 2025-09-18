# ROC Strategy

## Overview
The ROC strategy is a StockSharp port of the MetaTrader expert advisor stored in `MQL/26938/ROC.mq4`. It operates on a single symbol and evaluates price action using a chain of linear weighted moving averages (LWMA), a custom rate-of-change (ROC) model, higher timeframe momentum and a monthly MACD filter. The original money management features such as break-even, pip-based trailing stops, equity protection and money denominated profit targets are preserved.

## Entry logic
1. The strategy subscribes to three data streams:
   - Primary trading candles defined by the `CandleType` property.
   - A higher timeframe for the 14-period momentum oscillator (selected automatically according to the trading timeframe).
   - Monthly candles for the MACD confirmation filter.
2. On each finished trading candle the following conditions must be satisfied to open a position:
   - The custom ROC model must report an uptrend (`Line4 < Line5`) for buys or a downtrend (`Line4 > Line5`) for sells.
   - The fast LWMA calculated on typical price must trade above the slow LWMA for buys and below for sells.
   - Any of the last three momentum readings taken from the higher timeframe must exceed the configured buy or sell threshold (absolute deviation from 100).
   - The monthly MACD main line must stay above its signal line for buys and below for sells.
   - Position sizing respects the `MaxTrades` limit and optionally scales the next trade volume after consecutive losses when `IncreaseFactor` is greater than zero.

## Exit logic
- Classic stop-loss and take-profit orders are projected in MetaTrader points as soon as the position size changes.
- The optional break-even block moves the protective stop to the entry price plus the configured offset once the trigger distance in points is reached.
- Pip-based trailing stops tighten the stop value on every candle close.
- Money management checks close the position when a currency target or percentage target is reached, and can trail floating profit by detecting pullbacks larger than `StopLossMoney` after the profit exceeds `TakeProfitMoney`.
- An equity stop compares the floating drawdown with the highest recorded equity and liquidates the position when the allowed percentage is exceeded.
- Setting `ExitStrategy` to `true` performs the emergency exit routine and closes the current position at market.

## Parameters
| Name | Description |
| --- | --- |
| `LotSize` | Base trade volume opened on each signal. |
| `IncreaseFactor` | Recalculates the next volume after consecutive losing trades. |
| `FastMaPeriod` / `SlowMaPeriod` | Length of the LWMA trend filters. |
| `PeriodMa0`, `PeriodMa1`, `BarsV`, `AverBars`, `KCoefficient` | Define the custom ROC trend model. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Minimum absolute deviation from 100 used by the higher timeframe momentum filter. |
| `StopLossSteps`, `TakeProfitSteps` | Initial protective distances expressed in MetaTrader points. |
| `TrailingStopSteps` | Pip-based trailing stop. |
| `UseBreakEven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Configure the break-even module. |
| `UseTpInMoney`, `TpInMoney`, `UseTpInPercent`, `TpInPercent` | Money and percent based take profit targets. |
| `EnableMoneyTrailing`, `TakeProfitMoney`, `StopLossMoney` | Money trailing module parameters. |
| `UseEquityStop`, `TotalEquityRisk` | Equity protection settings. |
| `MaxTrades` | Maximum number of scale-ins per direction. |
| `ExitStrategy` | Forces an immediate flat position when enabled. |

## Notes
- The higher timeframe for the momentum indicator is automatically derived from the trading timeframe to match the original switch statement in the MetaTrader code.
- All indicator calculations use the high level `Bind` API, therefore no manual data requests are required.
- The strategy is netting-only: when a new long signal appears while holding shorts, the short exposure is closed first before entering long, mirroring the behaviour of the original EA on non-hedging accounts.
