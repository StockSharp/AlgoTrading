# Linear Regression Channel (Fibo) Strategy

## Overview
This strategy is the StockSharp conversion of the MetaTrader expert advisor **"linear regression channel"**. It trades in the direction of the higher-timeframe linear trend confirmed by a set of weighted moving averages, momentum readings, and a monthly MACD filter. Money-management rules replicate the original behaviour with floating profit targets, trailing of accumulated gains, break-even protection, and an equity stop.

## Trading Logic
1. **Primary timeframe** – configurable candle type (default 15-minute). All signal calculations run on this timeframe.
2. **Trend filter** – a fast and a slow linear weighted moving average (LWMA) calculated on the typical price. Long signals require the fast LWMA to be above the slow LWMA; short signals require the opposite.
3. **Momentum confirmation** – the momentum indicator is evaluated on a higher timeframe that mirrors the original MetaTrader mapping (M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1). The last three momentum values are converted to the absolute distance from the 100 level. A long setup needs any of the three distances to exceed the bullish threshold, while a short setup needs any of the three to exceed the bearish threshold.
4. **Monthly MACD bias** – monthly candles drive a MACD(12,26,9) filter. Long trades are only allowed when the MACD main line is above its signal line; short trades require the opposite relationship.
5. **Entry condition** – when all filters align and trading is allowed, the strategy opens a market order in the corresponding direction. The current position is closed and reversed when an opposite signal is produced.

## Risk and Trade Management
- **Fixed stop-loss / take-profit** – distances are defined in instrument points and applied to every entry. If the candle high/low pierces these levels, the position is closed.
- **Trailing stop** – optional; activates once the position gains a configurable amount of points and trails the best price by the specified offset.
- **Break-even** – optional; after the price advances by the trigger distance, the stop level is moved to the entry price plus/minus an offset to secure profits.
- **Floating profit take-profit** – optional monetary target. When the net unrealised profit (expressed in account currency) exceeds the threshold, all positions are closed.
- **Percent-based take-profit** – optional target based on the initial equity at the moment the strategy starts.
- **Money trailing** – once floating profit reaches the trigger, the strategy records the peak profit. If profit retraces by the specified stop amount, the position is closed.
- **Equity stop** – optional drawdown protection. While the position is losing, if the floating loss exceeds a percentage of the observed equity peak, the strategy liquidates the position.

## Parameters
| Name | Description |
| ---- | ----------- |
| `Candle Type` | Primary timeframe for signal generation. |
| `Fast LWMA` / `Slow LWMA` | Periods for the fast and slow linear weighted moving averages. |
| `Momentum Length` | Momentum lookback length on the higher timeframe. |
| `Momentum Buy Threshold` / `Momentum Sell Threshold` | Minimum absolute distance from 100 required for bullish/bearish momentum confirmation. |
| `Take Profit (points)` / `Stop Loss (points)` | Protective distances expressed in instrument points. |
| `Use Trailing`, `Trailing Activation`, `Trailing Offset` | Trailing stop configuration. |
| `Use Break-even`, `Break-even Trigger`, `Break-even Offset` | Break-even logic parameters. |
| `Max Trades` | Maximum number of sequential entries allowed during the run. |
| `Order Volume` | Base volume for market orders. |
| `Use Money TP`, `Money Take Profit` | Floating monetary take-profit. |
| `Use Percent TP`, `Percent Take Profit` | Take-profit calculated as a percentage of initial equity. |
| `Enable Money Trailing`, `Money Trailing Trigger`, `Money Trailing Stop` | Trailing of floating profit. |
| `Use Equity Stop`, `Equity Risk %` | Equity-based stop-loss guard. |

## Notes
- The strategy keeps only one net position (long or short) and reverses when an opposite signal arrives.
- Momentum and MACD subscriptions automatically add the necessary higher timeframes to the data feed through `GetWorkingSecurities()`.
- All comments inside the code are in English per repository guidelines.
