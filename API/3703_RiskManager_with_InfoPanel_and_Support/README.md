# Risk Manager with Info Panel and Support

## Overview

`RiskManagerWithInfoPanelAndSupportStrategy` is a utility strategy converted from the MQL5 script **RiskManager_with_InfoPanel_and_Support_noDLL**. It does not place orders automatically. Instead, it periodically logs a detailed account snapshot that mirrors the original on-chart information panel and keeps the support button message available in the Designer log.

The strategy calculates the position size required to keep risk per trade under control, evaluates the reward-to-risk ratio, and monitors the realized daily profit versus a configurable daily loss cap. Whenever the cap is violated the log explicitly warns the operator to stop trading, just like the MetaTrader version blocked further actions.

## How it works

1. When the strategy starts it validates that a portfolio is attached and launches a `System.Threading.Timer`. The timer fires immediately and repeats every `UpdateIntervalSeconds` seconds.
2. Each timer tick gathers live portfolio values (balance, equity, floating P/L) together with the configured entry price and percentage-based stop/take distances.
3. The helper re-creates the information panel text using `StringBuilder` and writes it to the Designer log. Placement, font and color settings are logged to keep the configuration visible even though no visual panel is created inside StockSharp.
4. If the optional support panel is enabled the support message and the Bybit referral link are added to the log output, emulating the clickable button from the MQL chart.
5. Daily performance is tracked by storing the PnL value at the first tick of every new session. The current PnL minus that base equals the daily profit displayed by the report.

The strategy never submits orders. Its only purpose is to provide permanent visibility into account health and recommended volumes before the trader opens positions manually or via other strategies.

## Key calculations

- **Risk amount per trade** = `PortfolioValue * RiskPercent / 100`. `PortfolioValue` prefers `Portfolio.CurrentValue` and falls back to `Portfolio.BeginValue` if the broker model does not publish live equity.
- **Stop-loss price** = `EntryPrice - EntryPrice * (StopLossPercent / 100)`.
- **Take-profit price** = `EntryPrice + EntryPrice * (TakeProfitPercent / 100)`.
- **Recommended volume** = `RiskAmount / (StopDistanceInSteps * StepPrice)`, rounded up to the nearest `VolumeStep` so the broker accepts the order size.
- **Reward-to-risk ratio** uses the stop and take distances expressed in price steps.
- **Daily profit** = `PnL - DailyPnLBase`. `DailyPnLBase` is reset the first time a new calendar day is seen by the timer or by any PnL update event.
- **Daily risk limit** = `Equity * MaxDailyRiskPercent / 100`. If the realized daily profit drops below the negative limit, a warning is appended to the log entry.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `RiskPercent` | 1 | Risk per trade as account percentage. |
| `EntryPrice` | 1.1000 | Reference entry price used for risk computations. |
| `StopLossPercent` | 0.2 | Stop-loss distance relative to the entry price. |
| `TakeProfitPercent` | 0.5 | Take-profit distance relative to the entry price. |
| `MaxDailyRiskPercent` | 2 | Maximum tolerated loss per day; exceeding this prints a warning. |
| `UpdateIntervalSeconds` | 10 | Timer period for refreshing the report. |
| `InfoPanelXDistance` | 10 | Virtual X offset of the information panel (logged only). |
| `InfoPanelYDistance` | 10 | Virtual Y offset of the information panel (logged only). |
| `InfoPanelWidth` | 350 | Virtual width of the panel in pixels. |
| `InfoPanelHeight` | 300 | Virtual height of the panel in pixels. |
| `InfoPanelFontSize` | 12 | Font size reported for the panel. |
| `InfoPanelFontName` | Arial | Font family reported for the panel. |
| `InfoPanelFontColor` | White | Font color reported for the panel. |
| `InfoPanelBackColor` | DarkGray | Background color reported for the panel. |
| `UseSupportPanel` | true | Enables or disables the support message section. |
| `SupportPanelText` | Need trading support? Contact us! | Text logged for the support panel. |
| `SupportPanelFontColor` | Red | Font color reported for the support section. |
| `SupportPanelFontSize` | 10 | Font size reported for the support section. |
| `SupportPanelFontName` | Arial | Font family reported for the support section. |
| `SupportPanelXDistance` | 10 | Virtual X offset of the support button. |
| `SupportPanelYDistance` | 320 | Virtual Y offset of the support button. |
| `SupportPanelXSize` | 250 | Virtual width of the support button. |
| `SupportPanelYSize` | 30 | Virtual height of the support button. |

## Usage notes

- Attach the strategy to a portfolio that exposes either `CurrentValue` or `BeginValue` so the risk calculations have a capital base.
- Adjust `EntryPrice`, `StopLossPercent`, and `TakeProfitPercent` before every planned trade to obtain an accurate lot-size recommendation.
- Keep `UpdateIntervalSeconds` positive. Setting it to a very small value may flood the log; the default of 10 seconds mirrors the original script.
- The strategy is informational only. Combine it with discretionary trading or other StockSharp strategies to execute the actual orders.
- The Bybit support link is logged for reference; there is no embedded browser button inside the Designer environment.

## Differences vs. the MQL5 version

- The StockSharp port writes all information to the log instead of painting on-chart graphical objects.
- Timer execution is handled by `System.Threading.Timer`, respecting StockSharp threading rules and preventing overlapping executions.
- Daily profit resets occur automatically when the trading day changes or when PnL updates arrive, ensuring the reported figure stays in sync with the account state.
- Position sizing respects `VolumeStep` rounding so the recommended size is always acceptable for the broker model.
