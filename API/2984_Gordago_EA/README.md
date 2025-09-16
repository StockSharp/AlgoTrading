# Gordago EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A port of the historical "Gordago EA" MetaTrader 5 expert advisor. The strategy trades the base timeframe (default M3) while reading MACD signals from a higher intraday chart and a stochastic filter from an hourly chart. It preserves the original stop/take parameters and trailing logic, but uses the StockSharp high-level API for data subscriptions and order management.

## Strategy Logic

- **Market data**
  - Main execution candles: configurable, default three-minute candles.
  - MACD candles: configurable, default twelve-minute candles.
  - Stochastic candles: configurable, default one-hour candles.
- **Indicators**
  - MACD (fast 12, slow 26, signal 9) computed on the MACD timeframe.
  - Stochastic oscillator (length 5, %K smoothing 3, %D 3) computed on the stochastic timeframe.
- **Entry conditions**
  - **Buy**: current MACD value above the previous one, previous MACD below zero, stochastic %K below the buy threshold (default 37) and rising compared to the prior value.
  - **Sell**: current MACD value below the previous one, previous MACD above zero, stochastic %K above the sell threshold (default 96) and falling compared to the prior value.
- **Order placement**
  - Order volume is fixed; switching direction automatically offsets any opposite position before opening a new one.
  - Separate stop-loss/take-profit distances exist for long and short trades (defaults: 40/70 pips for long, 10/40 pips for short).
- **Exits**
  - Protective stop-loss and take-profit levels are checked on every finished base candle.
  - A trailing stop activates when price advances beyond the configured trailing distance plus trailing step; once triggered it keeps ratcheting toward the market by the trailing distance.
  - Trailing can introduce a protective stop even when the original stop was disabled, mirroring the source EA.

## Parameters

- `OrderVolume` – trade volume in lots.
- `StopLossBuyPips` / `TakeProfitBuyPips` – long-side stop-loss and take-profit distances (in pips).
- `StopLossSellPips` / `TakeProfitSellPips` – short-side stop-loss and take-profit distances (in pips).
- `TrailingStopPips` – trailing distance in pips; set to zero to disable trailing.
- `TrailingStepPips` – minimum additional profit (in pips) before the trailing stop can advance.
- `StochasticBuyLevel` / `StochasticSellLevel` – oscillator thresholds for long and short entries.
- `CandleType` – working timeframe for execution logic.
- `MacdCandleType` – timeframe used to feed the MACD indicator.
- `StochasticCandleType` – timeframe used to feed the stochastic oscillator.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD periods.
- `StochasticLength`, `StochasticSignalPeriod`, `StochasticSmoothing` – stochastic oscillator periods.

## Implementation Notes

- Pip distances are converted to prices using the security's `PriceStep`. If the step has three or five fractional digits the strategy multiplies it by ten, reproducing the pip adjustment used in the original MQL implementation for 3/5-digit forex quotes.
- Trailing stop is ignored when `TrailingStopPips` is positive but `TrailingStepPips` is not; a warning is logged in that case.
- Because the StockSharp version works on candle close events, protective logic executes once per finished candle instead of on every tick as in the MT5 version. The trade management behaviour otherwise follows the original rules.
- Only the C# implementation is provided; no Python translation or folder is included by request.
