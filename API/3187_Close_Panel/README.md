# Close Panel Strategy

## Overview
This strategy ports the MetaTrader 5 utility **Close panel (in money)** to StockSharp. The original expert displays a panel with
three buttons that let the trader close all positions, close the losing ones whose drawdown exceeds a configured amount and
close winning positions that already reached the desired profit. The StockSharp version reproduces the same logic without the
manual interface by providing parameters that automatically trigger the corresponding exit routines.

## How it works
- At start the strategy can optionally flatten every open position. This mirrors the "Close all" button and is controlled by the
  `CloseAllOnStart` parameter.
- While running the strategy subscribes to tick data and monitors the floating PnL of each portfolio position.
- When `CloseLossEnabled` is true, any position whose unrealized loss (expressed in money) is below the negative threshold is
  closed with a market order.
- When `CloseProfitEnabled` is true, positions whose floating profit exceeds the configured target are immediately secured with a
  market order.
- The strategy automatically prevents duplicated exit orders by checking for active opposite orders before submitting a new one.

## Order handling
All exits are performed with market orders in the direction required to flatten the exposure (sell for long positions, buy for
short positions). The routine collects volumes from the strategy itself, the hosting portfolio and child strategies to ensure
that every open position on the account is processed.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `LossThreshold` | Maximum allowed unrealized loss per position (in portfolio currency). Positions whose PnL falls below `-LossThreshold` are closed when `CloseLossEnabled` is active. |
| `ProfitThreshold` | Profit target per position (in portfolio currency). Positions whose PnL rises above this level are closed when `CloseProfitEnabled` is active. |
| `CloseLossEnabled` | Enables the automatic loss-protection rule. |
| `CloseProfitEnabled` | Enables the automatic profit-protection rule. |
| `CloseAllOnStart` | Sends market orders to close every open position right after the strategy starts. |

## Notes
- The original panel required manual button presses. Use the boolean parameters to reproduce the desired combination of actions
  when the StockSharp strategy is launched.
- Thresholds are specified in money (deposit currency). Adjust them to match the account size and instrument volatility.
- Because trades are executed with market orders, slippage may cause the final PnL to differ slightly from the requested amount.
- The strategy does not open new positions; it only manages and closes existing exposure.
