# ValidateMe Strategy

## Overview

The ValidateMe strategy ports the basic validation framework from the original MQL4 expert advisor. The logic focuses on checking the availability of funds, verifying that stop-loss and take-profit distances respect exchange constraints, and then firing a single market order in the chosen direction. The strategy continuously monitors trade execution events and opens a new position only when no positions or active orders are present.

## Trading Logic

1. The strategy subscribes to tick data of the configured security.
2. When the strategy is online, formed, and trading is allowed, it verifies that no open position and no active orders are present.
3. It then sends a market order in the configured direction (buy or sell) using the defined lot size.
4. A protective module immediately attaches take-profit and stop-loss orders calculated from pip distances, ensuring compliance with broker stop levels (adjusted for fractional pricing).
5. Once the position closes, the strategy waits for the next tick and repeats the validation before sending a new order.

## Parameters

| Parameter | Description |
| --- | --- |
| **Take Profit (pips)** | Distance from the entry price to the take-profit in pips. Must be greater than zero. |
| **Stop Loss (pips)** | Distance from the entry price to the stop-loss in pips. Must be greater than zero. |
| **Lots** | Trade volume in lots used for every market order. |
| **Direction** | Direction of the market order (Buy or Sell). |

## Risk Management

* The strategy uses `StartProtection` with absolute offsets to register both take-profit and stop-loss orders.
* Pip size is calculated from the security price step and decimal precision to mimic MetaTrader behavior (5- and 3-digit symbols use a tenfold point size).
* The strategy fires new orders only if no existing orders are active, avoiding order stacking.

## Usage Notes

* Attach the strategy to a security and set the desired volume and direction.
* Configure the take-profit and stop-loss distances in pips according to broker requirements.
* The strategy does not rely on indicators and is intended as a validation framework rather than a full trading system.
* Portfolio risk control (e.g., max drawdown) can be combined externally if required.
