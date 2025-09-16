# Basic CCI RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Basic CCI RSI strategy reproduces the original MetaTrader expert advisor that waits for both the Commodity Channel Index (CCI) and Relative Strength Index (RSI) to confirm momentum for two consecutive closed candles before entering a trade. The StockSharp version keeps the pip-based money management rules, converts them into price steps automatically, and adds the same trailing-stop behaviour that was implemented with position modifications in MQL5.

## How the strategy trades

1. At the close of each candle (hourly by default) the strategy receives fresh CCI and RSI values.
2. Long entries require **both** indicators to stay above their respective upper thresholds for the current and the previous closed candle. Short entries require both to stay below their lower thresholds for the last two candles.
3. When a signal occurs the strategy opens a position with the configured volume (closing any opposite exposure) and immediately calculates fixed stop-loss and take-profit prices using the pip distances from the original script.
4. While the position is open the strategy constantly checks whether the candle range touched the stop or take levels and exits at market if either is hit.
5. A trailing stop replicates the MetaTrader implementation: once profit exceeds `TrailingStopPips + TrailingStepPips`, the protective stop is moved to `TrailingStopPips` behind the current close (for longs) or above it (for shorts). Further adjustments require an additional `TrailingStepPips` of profit before tightening again.

This flow keeps the logic close to the source MQL5 expert while using StockSharp's high-level candle subscriptions and indicators.

## Risk management

- **Stop-loss**: fixed pip distance converted to the instrument's price step. Disabled when set to zero.
- **Take-profit**: fixed pip distance converted to the instrument's price step. Disabled when set to zero.
- **Trailing stop**: optional pip distance with a step buffer that mimics the expert advisor's `Trailing()` function. Disabled when `TrailingStopPips` is zero.
- **Position sizing**: controlled through the strategy `Volume` property; the default lot is one contract.

## Parameters

| Name | Description |
| --- | --- |
| `StopLossPips` | Distance in pips between the entry price and the stop-loss order. |
| `TakeProfitPips` | Distance in pips between the entry price and the take-profit target. |
| `TrailingStopPips` | Profit (in pips) required to start trailing the stop. |
| `TrailingStepPips` | Additional profit (in pips) required before each new trailing adjustment. |
| `CciPeriod` | Averaging period for the CCI indicator. |
| `RsiPeriod` | Averaging period for the RSI indicator. |
| `RsiLevelUp` | Overbought level that must be exceeded to validate long trades. |
| `RsiLevelDown` | Oversold level that must be broken to validate short trades. |
| `CciLevelUp` | Upper CCI threshold that confirms bullish momentum. |
| `CciLevelDown` | Lower CCI threshold that confirms bearish momentum. |
| `CandleType` | Timeframe used for candle aggregation and indicator calculations. |

## Default values

- `StopLossPips` = 125
- `TakeProfitPips` = 60
- `TrailingStopPips` = 5
- `TrailingStepPips` = 5
- `CciPeriod` = 12
- `RsiPeriod` = 15
- `RsiLevelUp` = 75
- `RsiLevelDown` = 30
- `CciLevelUp` = 80
- `CciLevelDown` = -95
- `CandleType` = 1 hour candles

## Additional notes

- Pip distances are scaled automatically: if the instrument uses 3 or 5 decimal places the strategy multiplies the price step by ten, matching the MetaTrader "adjusted point" logic.
- Entries are evaluated only on closed candles to avoid repainting and to mirror the original "new bar" condition in the expert advisor.
- Exits always use market orders, providing deterministic behaviour inside the StockSharp backtesting environment.

## Classification tags

- Category: Oscillator confirmation
- Direction: Bi-directional
- Indicators: CCI, RSI
- Stops: Fixed and trailing (pip based)
- Complexity: Beginner
- Timeframe: Intraday to swing (default 1 hour)
- Seasonality: No
- Neural networks: No
- Divergence: No
- Risk level: Moderate
