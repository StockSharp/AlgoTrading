# Multi Strategy Combo Strategy

## Overview
The **Multi Strategy Combo Strategy** is a C# conversion of the MetaTrader 4 "Multi-Strategy iFSF" expert advisor. The original EA combines multiple indicators (MA, RSI, MACD, Stochastic, SAR) and wraps them with trend, Bollinger range and noise filters. The StockSharp port preserves the same idea using high-level `SubscribeCandles().Bind(...)` streams and indicator classes. Every enabled indicator produces a BUY/SELL vote; only when all votes agree does the strategy execute an order. Additional filters emulate the EA's combo modes.

## Core logic
* **Consensus engine** – Moving averages, RSI, MACD, Stochastic and Parabolic SAR each provide a discrete signal. If all enabled indicators agree on BUY (or SELL) the consensus becomes bullish (or bearish).
* **Combo factor (1–3)** – Mirrors the EA's `Combo_Trader_Factor` logic. Each factor mixes consensus with ADX trend detection and Bollinger range confirmation differently:
  * *Factor 1* prefers trending conditions. Range states rely on Bollinger reversals when enabled.
  * *Factor 2* requires stronger confirmation: trend and range filters must agree with the consensus.
  * *Factor 3* is the strictest variant, demanding alignment between all active modules.
* **Trend detection** – ADX on a configurable timeframe labels the market as trending up/down or ranging up/down.
* **Bollinger filter** – Uses medium (2σ) and wide (3σ) bands. Long signals require a bounce from the lower band confirmed by recent oversold RSI values; shorts mirror the behaviour on the upper band.
* **Noise filter** – ATR-based check that blocks new trades when volatility is too small (replacement for the Damiani Volatmeter).
* **Auto-close** – When enabled the strategy instantly exits if the consensus flips to the opposite direction.

## Indicators and signals
* **Moving averages** – Three configurable MAs (method + length). Modes 1–5 reproduce the original crossover combinations (fast vs mid, mid vs slow, aggregated logic).
* **RSI** – Modes 1–4 cover overbought/oversold, momentum, combined and zone checks. All thresholds are adjustable.
* **MACD** – Four modes mimic the EA: trend slope, histogram cross while below/above zero, combined confirmation and signal line zero crossing.
* **Stochastic oscillator** – Either simple %K vs %D cross or cross with high/low thresholds.
* **Parabolic SAR** – Optional directional vote, supporting the "remember last signal" behaviour to avoid multiple triggers per trend.

## Risk management
* Optional stop-loss/take-profit offsets (absolute price distance) configured via `StopLossOffset` and `TakeProfitOffset`.
* Built-in trailing stop support through the StockSharp `StartProtection` helper.
* Daily position protection follows the base `Strategy` mechanics; no manual lot management is required.

## Key parameters
* **General** – `ComboFactor`, `CandleType`.
* **Moving averages** – `UseMa`, `MaMode`, individual lengths/methods, candle timeframe, "remember last" flag.
* **RSI** – `UseRsi`, `RsiMode`, `RsiPeriod`, overbought/oversold levels, zone thresholds, "remember last" flag.
* **MACD** – `UseMacd`, `MacdMode`, fast/slow/signal lengths, candle timeframe, "remember last" flag.
* **Stochastic** – `UseStochastic`, smoothing parameters, thresholds and candle timeframe.
* **SAR** – `UseSar`, acceleration settings, candle timeframe.
* **Trend filter** – `UseTrendDetection`, `AdxPeriod`, `AdxLevel`, candle timeframe.
* **Bollinger filter** – `UseBollingerFilter`, `BollingerPeriod`, medium/wide deviations, RSI range length.
* **Noise filter** – `UseNoiseFilter`, `NoiseAtrLength`, `NoiseThreshold`, candle timeframe.
* **Auto close & risk** – `UseAutoClose`, `AllowOppositeAfterClose`, `StopLossOffset`, `TakeProfitOffset`, `UseTrailingStop`.

All parameters are exposed as `StrategyParam<T>` to support optimisation, validation and UI grouping.

## Differences vs the MT4 EA
* Only StockSharp built-in indicators are used. The original option between ZeroLag and classic MACD is replaced with the native MACD implementation.
* All moving averages and oscillators operate on candle close prices. Price-type and shift offsets from MT4 (e.g., `FastMa_Price`, `FastMa_Shift`) are not available.
* The Damiani noise filter is approximated with ATR; the behaviour can be tuned via `NoiseThreshold`.
* Money management and order re-tries are handled by StockSharp (no manual `OrderSend` loops). The strategy works with aggregated positions (`BuyMarket`/`SellMarket`).
* The EA's comment panel and chart objects are omitted; instead logging is available through `LogInfo`.

## Usage
1. Add the `MultiStrategyComboStrategy` class to your StockSharp solution and compile.
2. Instantiate the strategy, set `Security`, `Portfolio` and desired `Volume`.
3. Configure timeframes for each indicator if multi-timeframe confirmation is required (defaults follow the EA's inputs).
4. Optionally adjust stop/take offsets, trailing behaviour and filter thresholds.
5. Start the strategy. Trades will trigger on closed candles when all enabled modules agree according to the selected combo factor.

## Conversion notes
* The strategy relies exclusively on high-level subscription APIs (`SubscribeCandles().Bind(...)`) – no manual indicator buffers are used.
* Tabs are used for indentation per repository guidelines.
* Extensive inline comments highlight how EA concepts map to StockSharp code.
