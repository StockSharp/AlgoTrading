# Buy Sell Close Strategy

## Summary
- Manual trading helper translated from the MetaTrader `@Buy_Sell_Close` panel.
- Provides toggles for market buy/sell orders and for closing long, short or all exposure.
- Supports automatic money management sized by risk-per-ten-thousand and applies stop-loss / take-profit brackets on demand.
- Logs portfolio snapshots to emulate the dashboard in the original MQL script.

## How it works
The original expert advisor rendered a set of chart buttons that could open trades, close them and update protective levels. In StockSharp the user interacts with strategy parameters instead of on-chart buttons. Every boolean control is a one-shot trigger: set it to `true` to execute the action, after execution the strategy resets the parameter back to `false`.

### Key actions
- **Open Buy / Open Sell** – submit market orders. The volume is calculated with either fixed lots or the automatic money management formula (`balance * risk / 10000`).
- **Close Buys / Close Sells** – flatten long or short exposure respectively. Hedged positions are supported because the strategy tracks long and short legs separately.
- **Close Everything** – closes both long and short inventory.
- **Apply Stops** – cancels existing protective orders and places fresh stop-loss and take-profit brackets for both long and short sides using the configured distances.
- **Refresh Snapshot** – writes a detailed log entry with balance, equity, volumes and floating PnL, replicating the informational labels from the MetaTrader panel.

### Automatic money management
When `Automatic Money Management` is enabled the strategy computes the trade size from the portfolio balance and the `Risk (1/10000)` parameter. The risk value matches the MetaTrader implementation (e.g., `0.2` equals 0.02% of balance). If contract metadata lacks `StepPrice` information the strategy falls back to the fixed volume.

### Stop-loss / take-profit handling
Stops and targets are expressed in MetaTrader points. They are translated to price distances by multiplying with `PriceStep`. The strategy cancels previous protective orders before submitting new ones to avoid duplicates. Protective orders are only created when the corresponding side has a non-zero volume.

### Logging behaviour
- Snapshot logs contain balance, equity, long/short volume and floating profit, providing the same visibility as the chart labels of the original panel.
- Informational warnings are emitted whenever a requested action cannot be executed (missing portfolio/security, zero volume, etc.).

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Automatic Money Management` | Toggle for using the risk-based sizing formula. | `true` |
| `Risk (1/10000)` | Risk share of the balance, identical to the MetaTrader input. | `0.2` |
| `Fixed Volume` | Manual volume used when automatic sizing is disabled. | `0.01` |
| `Stop-Loss Points` | Stop distance in points. Used when applying protection orders. | `250` |
| `Take-Profit Points` | Take profit distance in points. Used when applying protection orders. | `500` |
| `Open Buy`, `Open Sell` | One-shot triggers for sending market orders. | `false` |
| `Close Buys`, `Close Sells`, `Close Everything` | One-shot triggers for flattening exposure. | `false` |
| `Apply Stops` | One-shot trigger to place stop-loss and take-profit orders. | `false` |
| `Refresh Snapshot` | Request a log entry with current account metrics. | `false` |

## Differences vs. the MQL version
- Interaction happens through the StockSharp parameter panel instead of chart buttons.
- Stop/target orders are placed with StockSharp high-level API helpers, not by modifying existing MetaTrader tickets.
- Account information is written to the log instead of rendering chart labels.
- Risk sizing reuses the original ten-thousandth formula while respecting StockSharp security metadata (volume steps, min/max volume).

