# Fed Model

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Universe order is [equity ETF, optional cash ETF]. Use only daily candles and the first security's exchange calendar. At the last trading day's close of each month, require RegressionMonths consecutive monthly observations published by that close. Regress y = EarningsYield − BondYield on x = 0..N−1 with an intercept: slope = Σ((x−mean(x))×(y−mean(y))) / Σ(x−mean(x))²; forecast = mean(y) + slope×(N−mean(x)). Positive holds equities; negative holds the cash ETF or uninvested cash. Zero leaves the previous regime unchanged. Trade only when the forecast regime changes, without monthly resizing within one regime. Fully liquidate the old regime before buying the next.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

The publication cutoff is the latest actual CloseTime among synchronized finished candles. A trading session can finish before the configured candle interval has elapsed.

## Input data

YieldHistoryFile is required CSV: `Month,PublishedAt,EarningsYield,BondYield`. Month uses `yyyy-MM`; PublishedAt uses UTC `yyyy-MM-ddTHH:mm:ssZ`. Both yields use the same units. Only revisions published by the decision close are eligible. Require consecutive months ending in the decision month; a missing observation delays that month's forecast. Duplicate revisions and publication timestamps before the observation month are rejected. Series come from observed market/fundamental data, not inverse prices or candle ranges.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `RegressionMonths` | `12` |
| `YieldHistoryFile` | required |

## Execution

Universe is explicit. Execution prices and Capital use USD per traded unit. Long-only exposure is capped at min(Capital, reported Portfolio.CurrentValue); an unavailable/nonpositive portfolio value leaves no buying budget. No leverage, stop loss or take profit.

Round target quantities toward zero to VolumeStep. Wait for actual liquidation fills before opening new positions; reconcile actual positions, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters nonzero target adjustments, while full liquidation bypasses it.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
