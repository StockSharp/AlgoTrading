# Consistent Momentum

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Compute long momentum P[d] / P[d − LookbackDays] − 1 and short momentum P[d] / P[d − ShortLookbackDays] − 1 using distinct daily dates. Require LookbackDays + 1 closes. Independently rank both measures: buy the intersection of their top Deciles buckets and short the intersection of their bottom buckets. Each extreme contains floor(universe count / Deciles) candidates. Create one tranche per month with gross budget Capital × Leverage / HoldingMonths, half per side, equal within each intersection; an empty side stays cash. Freeze each tranche's rounded units at creation. Expire it at the start of its creation month plus HoldingMonths, preserving other tranches. Sum live tranche quantities and trade only the change. Later HoldingMonths changes do not move existing expiry dates.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

For intraday CandleType, daily statistics use only UTC dates strictly before the current signal date. The incomplete current day is excluded. A finished one-day candle may contribute its own completed date.

## Input data

PriceHistoryFile is an optional CSV of positive daily closes: SecurityId,Date,Close. Dates use yyyy-MM-dd. Current and future dates cannot prefill a decision; live finished closes replace each UTC date. Duplicate dates are rejected. It is needed to warm long lookbacks when the execution archive is short.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `PriceHistoryFile` | optional — not supplied |
| `LookbackDays` | `147` |
| `ShortLookbackDays` | `21` |
| `HoldingMonths` | `6` |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

See the strategy-specific allocation above. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests use only packaged BTC/TON execution history. Explicit financial reports, option observations and price warmup fixtures test mechanics; they are not actual company fundamentals or option-market history. Reduced lookbacks, two buckets and March annual scheduling are identified test profiles, not proof of trading at unchanged long-horizon defaults. No return or profitability claim is made.
