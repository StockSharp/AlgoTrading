# Hybrid Scalper Strategy

## Overview

The **Hybrid Scalper Strategy** is a short-term trading algorithm converted from the MQL4 script `hybrid_Scalper.mq4`. It operates on the StockSharp high-level API and is designed for the 1-minute timeframe. The strategy combines multiple technical indicators to identify fast breakout opportunities while avoiding periods of excessive or insufficient volatility.

## Strategy Logic

1. **Trend Filter** – A fast EMA (21) and a slow EMA (89) determine market direction. Long trades are allowed only when the fast EMA is above the slow EMA; short trades require the opposite.
2. **Momentum Filter** – The Stochastic Oscillator (5,3,3) generates entry signals. A buy is triggered when %K is below 20 and below %D. A sell is triggered when %K is above 80 and still above %D.
3. **RSI Confirmation** – The Relative Strength Index with period 7 confirms momentum. Long entries require RSI below 25, while short entries require RSI above 85.
4. **Volatility Filter** – Bollinger Bands (50, deviation 4) measure current market width. The strategy trades only when the difference between the upper and lower bands is between 0.00045 and 0.00262, avoiding both quiet and unstable markets.
5. **Trading Days** – Parameters allow enabling or disabling trading for each weekday individually (Monday–Friday).

## Parameters

| Name | Description |
| ---- | ----------- |
| `RsiPeriod` | Period of the RSI indicator. |
| `EmaFastPeriod` | Fast EMA period for trend detection. |
| `EmaSlowPeriod` | Slow EMA period for trend detection. |
| `BbPeriod` | Period used in Bollinger Bands. |
| `BbDeviation` | Deviation multiplier for Bollinger Bands. |
| `TradeMonday`–`TradeFriday` | Enable trading on specific weekdays. |
| `CandleType` | Candle type/timeframe, default is 1-minute candles. |

## Notes

- The strategy uses the high-level `BindEx` API to connect multiple indicators in a single subscription.
- `StartProtection()` is invoked once on start to activate built-in position protection (no explicit stop-loss or take-profit parameters).
- All comments in the code are provided in English in accordance with repository guidelines.

## How to Run

1. Add the strategy file to a StockSharp project.
2. Configure the required market data and execution connectors.
3. Compile and launch the strategy; ensure that the selected instrument provides 1-minute candles.
4. Adjust parameters through the `StrategyParam` interface as needed.
