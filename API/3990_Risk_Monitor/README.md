# Risk Monitor Strategy

## Overview
Risk Monitor Strategy is a port of the MetaTrader 4 expert advisor `risk.mq4`. The original script never opened trades; instead it
determined how many lots the trader could safely deploy based on the account balance and a user-defined risk percentage. This
StockSharp version keeps the same spirit: it performs continuous account diagnostics, computes suggested trade sizes, monitors
floating and realized profits, and publishes the results directly into the strategy comment for quick decision making.

Unlike conventional strategies, Risk Monitor Strategy does not send orders automatically. Its role is supervisory: it gives the
trader a snapshot of current exposure, available capacity according to the chosen risk budget, and the profitability of closed
positions. The comment line is refreshed whenever positions, PnL, or trades change so the information always reflects the latest
portfolio state.

## Calculations
The strategy derives the figures displayed in the comment from three groups of data:

1. **Base lot size** – calculated as `AccountBalance / 1000` and aligned to the security volume step. This mirrors the original
   MT4 logic where every 1000 units of balance correspond to 1 standard lot.
2. **Risk lot size** – multiplies the base lots by `Risk % / 100`, aligns the result to the volume step, and represents how many
   lots may be opened while respecting the configured risk budget.
3. **Open lots & difference** – compares the absolute net position to the risk lot size. If the trader is below the threshold,
   the difference shows how many lots remain available before reaching the limit. A tiny negative difference that is smaller than
   the volume step is rounded to zero to avoid confusing noise.

For profits the strategy distinguishes between floating and realized values:

* **Floating PnL** – read from the strategy `PnL` property and expressed both in price units and as a percentage of the current
  portfolio value.
* **Realized profit** – accumulated from own trades. The component splits every closing fill into positive and negative parts,
  applies the reported commission, and keeps a running total. The final figure is also converted into a percentage of equity to
  match the MT4 readout.

## Parameters
* **Risk %** – portion of the account balance that can be committed to new positions. Default: `10`. The parameter is exposed for
  optimization so different risk budgets can be backtested quickly.

## Comment format
The strategy updates the comment with three lines:

1. `Base lots`, `Risk lots`, `Open lots`, `Lots to adjust` – quick view of position sizing metrics.
2. `Risk`, `Floating PnL` – risk setting, floating profit in currency units, and floating profit in percent of balance.
3. `Realized profit` – cumulative closed profit and its percentage.

All values are rounded similarly to the MT4 script, respecting the security lot step and using two decimal places for monetary
numbers. Because the output sits in the comment, it is immediately visible on the chart or in the strategy grid without opening
additional panels.

## Usage notes
* Attach the strategy to the instrument whose balance and position you want to supervise. It works with net positions (no MT4-style
  hedging) just like StockSharp itself.
* The strategy tolerates manual trading: it reacts to any trade confirmations to keep the statistics in sync.
* The comment is cleared automatically when the strategy stops or resets, preventing stale values from persisting across sessions.
* No Python implementation is provided; the API package contains only the C# version.
