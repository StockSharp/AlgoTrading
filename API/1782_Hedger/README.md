# Hedger Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy places a limit order and an opposite stop order to hedge the initial position. It works for both long and short modes and adds several risk controls.

The hedge order protects the main trade if price moves in the wrong direction. A 75-50 trailing rule can move the stop to half of the target once 75% of the profit goal is reached. Optional risk hedging can open a market order against the position after a large adverse move, and the stop can be tightened after a configurable number of ticks.

## Details

- **Entry Criteria**: Place limit order at `EntryPrice` and hedge stop at `EntryPrice ± Spread`.
- **Long/Short**: Configured via `IsLong`.
- **Exit Criteria**: Stop loss, take profit, 75-50 rule or risk hedge.
- **Stops**: Yes, with optional tightening.
- **Filters**: None.

## Parameters

- `EntryPrice` – price for the pending order.
- `StopLoss` – protective stop level.
- `TakeProfit` – profit target.
- `Volume` – order volume.
- `Spread` – distance for hedge order.
- `IsLong` – trade long if true, short if false.
- `UseRiskHedge` – open opposite market order on strong adverse move.
- `UseRiskSl` – tighten stop after adverse move of `RiskSlTicks`.
- `RiskSlTicks` – number of ticks for risk stop tightening.
- `UseRule7550` – enable 75-50 trailing rule.
