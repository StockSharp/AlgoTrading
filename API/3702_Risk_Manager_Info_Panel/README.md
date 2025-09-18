# Risk Manager Info Panel Strategy

## Overview
The **Risk Manager Info Panel Strategy** recreates the informational dashboard from the MetaTrader 5 expert *RiskManager_with_InfoPanel_and_Support*. The original robot did not place orders; it continuously calculated account risk metrics and displayed them in an on-chart panel together with a support button. The StockSharp conversion keeps the same analytical focus. It computes the recommended position size from the configured risk percentage, displays projected stop-loss and take-profit levels, and monitors whether the daily loss threshold has been exceeded. All results are published through the strategy comment (`Strategy.Comment`) so any StockSharp UI (Designer, Shell, custom dashboards) can render the panel.

The module does **not** open or close positions. Its purpose is to provide risk awareness while the trader executes orders manually or delegates them to other strategies.

## Key features
- Mirrors the MetaTrader calculation of lot size from equity, stop-loss distance and tick value.
- Tracks the current account balance, equity, floating PnL, and formatted timestamps.
- Computes risk-reward ratios, pip distances and currency exposure for the configured entry price.
- Aggregates realised PnL per day and warns when the drawdown exceeds the daily risk limit.
- Exposes an optional support message so the StockSharp comment reproduces the “Support” button text.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `RiskPercent` | `1` | Risk allocation per trade expressed as a percentage of portfolio equity. |
| `EntryPrice` | `1.1000` | Reference price used to derive stop-loss and take-profit projections. |
| `StopLossPercent` | `0.2` | Stop-loss offset in percent of the entry price. |
| `TakeProfitPercent` | `0.5` | Take-profit offset in percent of the entry price. |
| `MaxDailyRiskPercent` | `2` | Daily loss threshold. When realised PnL falls below `-equity × MaxDailyRiskPercent / 100`, a warning is shown. |
| `UpdateIntervalSeconds` | `10` | Frequency for refreshing the snapshot and updating the strategy comment. |
| `UseSupportMessage` | `true` | Enables the optional support/help line. |
| `SupportMessage` | `"Contact support for assistance!"` | Text appended after the statistics, emulating the MetaTrader support panel. |

All properties are editable at runtime through the Designer property grid or programmatically via `Strategy.Param`. Risk parameters are validated to avoid negative values, matching the guard clauses in the original MQL inputs.

## Usage
1. Attach the strategy to the desired instrument and portfolio.
2. Configure the reference `EntryPrice`, risk percentages, and the optional support message.
3. Start the strategy. Every `UpdateIntervalSeconds` seconds the comment is refreshed with a block similar to the MT5 info panel:
   ```
   Risk Manager for EURUSD@FX
   -----------------------------
   Account: DEMO-12345
   Balance: 10000.00
   Equity: 10025.34
   Floating PnL: 25.34
   Updated: 12:30

   Risk/Trade: 1.00%
   Entry Price: 1.10000
   Stop Loss: 1.09780 (0.20%)
   Take Profit: 1.10550 (0.50%)

   Distance (pips): 22.0
   Risk ($): 100.25
   Recommended Volume: 0.45
   Reward:Risk Ratio: 1.82

   Daily P/L: 35.00
   Daily Risk Limit: 200.51
   ```
4. Bind `Strategy.Comment` or the exposed `RiskSnapshot` string to your UI to reproduce the information panel. When the daily risk limit is breached the warning line `*** DAILY RISK LIMIT EXCEEDED! Trading suspended.` is appended automatically.

## Differences from the MetaTrader version
- Instead of chart objects, the StockSharp port writes the dashboard into `Strategy.Comment`. This keeps the logic platform-neutral and avoids UI-side dependencies.
- Tick and pip conversions rely on `Security.PriceStep` and `Security.StepPrice`, which makes the sizing formula work for futures, forex and CFDs without hard-coded point values.
- Daily realised PnL is tracked through `MyTrade.PnL` events. The counter resets automatically on the first update after midnight or when the first trade of a new day arrives.
- Support messages are text-only; there is no clickable button because StockSharp strategies do not manage WinForms chart overlays.

## Notes
- Assign a portfolio that reports `Portfolio.CurrentValue` and `Portfolio.CurrentBalance`. When those values are unavailable the strategy falls back to zero, so recommended sizes will be zero as well.
- Because the module is informational it never sends orders or calls `Buy/Sell` helpers. Use it together with manual trading or other automated strategies.
- The strategy starts the risk monitor once (no repeated `StartProtection` calls) in line with the general StockSharp guidelines for protective modules.
