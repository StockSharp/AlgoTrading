# e-Skoch Pending Orders Strategy

## Overview

The **e-Skoch Pending Orders Strategy** recreates the original MetaTrader expert advisor that waits for a new bar, analyses the two most recent highs and lows on both the trading timeframe and the daily timeframe, and places pending breakout orders. The goal is to catch momentum when the market breaks through the previous bar after a short-term pullback confirmed by the daily trend.

The StockSharp implementation keeps the original ideas but uses high-level API features such as candle subscriptions, automatic protection orders, and strategy parameters. The C# version is stored inside the `CS/` folder and no Python port is provided yet.

## Trading Logic

1. On every finished candle the strategy retrieves the highs and lows of the previous two candles on the working timeframe and the previous two daily candles.
2. If the last daily high is lower than the high from two days ago **and** the previous intraday high is lower than the one before it, the strategy places a **buy stop** above the latest intraday high plus a configurable buffer.
3. If the last daily low is higher than the low from two days ago **and** the previous intraday low is higher than the one before it, the strategy places a **sell stop** below the latest intraday low minus a configurable buffer.
4. Each pending order sets individual stop-loss and take-profit levels. When an entry is triggered the strategy immediately submits protective stop and limit orders for the open position.
5. When no positions or orders are active the strategy records the current equity as a baseline. If the account equity grows by the configured percentage relative to that baseline, all positions are closed and protective orders are cancelled.
6. Optional blocking (`CheckExistingTrade`) prevents new entries while any position is open, mirroring the original “CheckTrade” input parameter.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Primary timeframe used for signals. Default: 1-hour candles. |
| `TakeProfitBuyPips` / `StopLossBuyPips` | Long-side profit and loss offsets measured in pips. |
| `TakeProfitSellPips` / `StopLossSellPips` | Short-side profit and loss offsets measured in pips. |
| `IndentHighPips` / `IndentLowPips` | Distance in pips from the latest high or low used to place stop orders. |
| `CheckExistingTrade` | When true, new orders are skipped while any position is open. |
| `PercentEquity` | Percentage gain on equity required to exit all positions. |
| `Volume` | Order size (default 0.01 lot to match the original expert advisor). |

## Risk Management

- Buy stop orders place a stop-loss below the entry price and a take-profit above it.
- Sell stop orders place a stop-loss above the entry price and a take-profit below it.
- Protective orders are automatically cancelled when the position closes or when a new protection set is created.
- The equity growth check acts as a global “circuit breaker” to lock in profits before trading resumes.

## Notes

- The strategy requires both the trading timeframe and daily candles, so make sure data for both subscriptions is available in Designer or during backtests.
- Pip conversion automatically adjusts for symbols that use fractional pip pricing (3 or 5 decimal digits) by multiplying the price step by 10.
- The logic assumes a single aggregated position; simultaneous long and short exposure is intentionally avoided when `CheckExistingTrade` is enabled.
