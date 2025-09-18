# The Enchantress Strategy

## Overview

The Enchantress strategy replicates the self-learning behaviour of the MQL4 expert advisor with the same name. The original EA
classifies every finished candle into ten buckets, keeps a rolling history of the last seven buckets, and launches “virtual” buy
and sell orders for every new seven-candle pattern. Whenever price later touches the virtual take-profit or stop-loss levels, the
pattern receives a positive or negative score. Live trades are triggered only when the current seven-candle pattern belongs to the
top-performing virtual patterns. This StockSharp port preserves that feedback loop and exposes all critical configuration options
as strategy parameters.

## Candle classification

1. Every finished candle is evaluated once, using its open, close, high, and low prices.
2. The body direction splits candles into bearish (digits `0–4`) and bullish (digits `5–9`).
3. The high/low ratio `100 - Low * 100 / High` determines the exact digit within each group:
   - `0/5` for very small ranges (≤ 0.04)
   - `1/6` for small ranges (0.04 – 0.15)
   - `2/7` for medium ranges (0.15 – 0.25)
   - `3/8` for wide ranges (0.25 – 0.40)
   - `4/9` for extremely wide ranges (> 0.40)
4. The latest digit is appended to the rolling seven-character window that represents the current market pattern.

This classification matches the numeric buckets produced by the `ManagePatterns` routine of the original EA.

## Virtual order engine

- Once seven digits are available, the strategy creates a paired set of virtual orders (long and short) for the active pattern.
- Virtual entry price equals the candle close. Virtual stops and targets are derived from `VirtualStopLoss` and
  `VirtualTakeProfit` using the instrument price step.
- On subsequent candles the strategy checks if the candle high/low touches the virtual targets or stops:
  - A target hit contributes `+1` to the respective bullish or bearish score.
  - A stop hit contributes `-3` to the respective score, reproducing the penalty used by the EA.
- Closed virtual orders are discarded to keep memory usage bounded, while the accumulated scores remain attached to their
  seven-digit pattern key.

## Signal generation

Before processing the next candle, the strategy inspects the current seven-digit pattern (built from past candles only). Trading is
allowed Monday through Thursday; Fridays are skipped exactly like the MQL version. The following rules apply:

1. Build the ten best bullish and bearish patterns by score (only scores ≥ 1 are considered).
2. If the current pattern belongs to the bullish leader set, place a market buy. If it belongs to the bearish leader set, place a
   market sell. The same candle cannot trigger two entries because the strategy records the candle timestamp after the first fill.
3. After every decision the freshly completed candle is appended to the pattern window and the virtual orders for the new pattern
   are launched.

## Protective orders and sizing

- Real trades use `StopLoss` and `TakeProfit` distances expressed in pips. Both parameters are translated into price differences via
  the security price step and applied through `SetStopLoss`/`SetTakeProfit` right after the market order fills.
- Position sizing can operate in two modes:
  - **Fixed lot**: `LotSize` is used verbatim (adjusted to the exchange volume step/min/max constraints).
  - **Risk money management**: the volume equals `PortfolioValue / 100000 * RiskPercent`. This mirrors the original `AccountFreeMargin`
    formula and falls back to the fixed lot if no portfolio value is available.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `LotSize` | Fixed order size when money management is disabled. | `0.01` |
| `UseRiskMoneyManagement` | Toggle the dynamic sizing block. | `true` |
| `RiskPercent` | Percentage of portfolio value used in risk mode. | `15` |
| `StopLoss` | Real stop-loss distance in pips. | `60` |
| `VirtualStopLoss` | Stop distance used for virtual scoring. | `55` |
| `TakeProfit` | Real take-profit distance in pips. | `19` |
| `VirtualTakeProfit` | Take-profit distance for virtual scoring. | `25` |
| `CandleType` | Timeframe of the processed candles. | `5m` |

## Usage notes

- Ensure that the security metadata (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) is populated; otherwise sizing and pip
  conversions fall back to generic defaults.
- Portfolio valuation (`Portfolio.CurrentValue` or `Portfolio.BeginValue`) must be available for risk-based sizing to work.
- The strategy operates on finished candles only and does not perform intra-bar virtual order checks. The high/low comparison is the
  closest approximation of the tick-based logic from MT4.
- To warm up the pattern database faster, run the strategy on historical data in backtesting mode—the scoring logic is identical in
  both simulation and live trading.
