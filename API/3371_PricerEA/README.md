# PricerEA Strategy

## Overview

The **PricerEA Strategy** recreates the behaviour of the MetaTrader 4 expert "PricerEA v1.0" using the StockSharp high-level API.
It places up to four pending orders (buy stop, sell stop, buy limit and sell limit) at manually defined price levels. Once any
pending order is filled the strategy attaches protective stop-loss and take-profit orders, optionally enabling a trailing stop and
break-even adjustment to follow the original Expert Advisor.

## How it works

1. **Pending orders** – the strategy reads absolute price levels from the inputs and submits the corresponding pending orders only
   once at start-up. Optional expiration can be configured in minutes.
2. **Volume selection** – users may keep the fixed manual lot size or switch to the automatic mode where the volume is derived from
   the portfolio balance and the MT4 risk factor analogue.
3. **Protection** – after an entry order is filled the strategy creates stop-loss and take-profit orders at the configured distance
   (expressed in price points). When both trailing and break-even are enabled the stop follows the original MQL conditions: it is
   moved only after the price covers the break-even distance plus the initial stop.
4. **Order maintenance** – pending orders are cancelled automatically when their lifetime expires or when the strategy stops.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `BuyStopPrice`, `SellStopPrice`, `BuyLimitPrice`, `SellLimitPrice` | Absolute prices for the corresponding pending orders. A value of `0` disables the order. |
| `TakeProfitPoints` | Distance from the entry price to the take-profit order, measured in price points (`Security.PriceStep`). |
| `StopLossPoints` | Distance from the entry price to the stop-loss order, also measured in price points. |
| `EnableTrailingStop` | Enables the trailing stop logic. |
| `TrailingStepPoints` | Minimal movement (in points) required before the trailing stop is moved. |
| `EnableBreakEven` | Enables the break-even rule which lifts the stop above/below the entry after sufficient profit. |
| `BreakEvenTriggerPoints` | Extra profit (points) required before the stop is moved for break-even. |
| `PendingExpiryMinutes` | Lifetime of the pending orders in minutes. `0` keeps them alive until filled or manually cancelled. |
| `VolumeMode` | Chooses between manual volume and automatic sizing. |
| `RiskFactor` | Risk multiplier used by automatic sizing (mirrors the MQL input). |
| `ManualVolume` | Fixed lot size used when `VolumeMode` is set to `Manual`. |

## Differences vs. the MT4 version

- The automatic volume calculation uses the StockSharp portfolio balance and the security contract multiplier. Different brokers
  may use distinct formulas, therefore the resulting value can differ slightly from MetaTrader.
- Protective orders are placed via StockSharp helpers and respect the venue volume step, minimum and maximum volume.
- Expiration is implemented inside the strategy (MetaTrader relies on server-side order expiration).

## Usage notes

- Configure the price levels before starting the strategy. Values equal to zero leave the corresponding order disabled.
- To imitate the MT4 "Digits" logic the point-based parameters operate in `Security.PriceStep` units.
- Combine the strategy with StockSharp's portfolio and logging tools to monitor pending orders and protective stops.
