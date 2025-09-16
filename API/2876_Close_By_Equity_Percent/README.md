# Close by Equity Percent Strategy

## Overview
- **Category**: Risk management / account-level automation.
- **Original source**: MQL5 expert advisor "Close by Equity Percent" (#20880).
- **Purpose**: Monitor account equity versus the last flat balance and liquidate all open positions once equity grows to a configurable multiple of that balance.
- **Instruments**: Any securities already traded by other strategies or manual traders within the same portfolio.

## Core Idea
The original MQL expert advisor compares current account equity with the account balance (which only changes after positions are flat). When equity reaches or exceeds `Balance * EquityPercentFromBalance`, the script closes every open position to lock in gains. This StockSharp port keeps the same account-protection logic while integrating with the high-level strategy API.

## How It Works
1. When the strategy starts it snapshots the current portfolio value. This acts as the "balance" reference while the account is flat.
2. The strategy subscribes to 1-minute candles (configurable through `CandleType`) on the configured `Security`. The candle feed is only used as a timer to trigger equity checks.
3. On each finished candle:
   - If all positions are flat, the balance snapshot is refreshed to the latest portfolio value.
   - The current equity (`Portfolio.CurrentValue`) is compared to `balanceSnapshot * EquityPercentFromBalance`.
   - When equity meets or exceeds the threshold, every open position in the portfolio is closed via `ClosePosition(position.Security)`.
4. The balance snapshot is updated again once all positions are closed, allowing the cycle to restart.

## Parameters
| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `EquityPercentFromBalance` | decimal | 1.20 | Equity multiple that must be reached before liquidating all positions. Value `1.20` means "close everything when equity is 120% of the last flat balance". |
| `CandleType` | `DataType` | 1-minute time-frame candle | Data stream used solely to trigger periodic equity checks. Adjust to match the cadence you prefer for monitoring equity. |

## Implementation Notes
- Uses `Strategy.ClosePosition(Security)` for each open position, mirroring the `PositionClose` loop in the MQL version.
- Tracks the balance snapshot only after all positions are flat, reproducing how the MQL script relied on `AccountBalance` (which updates after positions are closed).
- The strategy is account-level: it does not open positions itself, and it will attempt to close **all** positions within the connected portfolio regardless of the symbol.
- Requires both `Portfolio` and `Security` to be assigned before starting. The security is only used to subscribe to candles that provide timing events.

## Usage Guidelines
1. Attach the strategy to the portfolio you wish to protect and set the `Security` whose candle stream you want to use as a timer (e.g., a highly liquid instrument).
2. Adjust `EquityPercentFromBalance` to the profit-taking multiple that suits your risk plan.
3. Start the strategy. Whenever equity reaches the specified multiple of the last flat balance, all open positions in the portfolio are closed automatically.
4. After liquidation, the balance snapshot updates, so the next profit cycle will again wait for equity to grow by the configured percentage before triggering another close-out.

## Practical Example
- Initial balance snapshot = 10,000 USD.
- `EquityPercentFromBalance = 1.2` → target equity = 12,000 USD.
- Open positions appreciate until equity hits 12,050 USD.
- Strategy closes every open position; balance snapshot refreshes once the portfolio is flat (e.g., to 12,000 USD).
- The next cycle waits for equity to exceed 12,000 * 1.2 = 14,400 USD before acting again.
