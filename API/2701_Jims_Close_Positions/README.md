# Jims Close Positions Strategy

## Overview
This strategy is a direct StockSharp conversion of the MetaTrader expert advisor **Jims Close Positions** from `MQL/19364`. The logic is designed to flatten existing positions on demand instead of generating new entries. The implementation focuses on simple execution management and follows the StockSharp high-level API guidelines.

The strategy can operate in three mutually exclusive modes:
1. Close every open position immediately.
2. Close only positions that are currently profitable.
3. Close only positions that are currently losing.

A validation step prevents multiple modes from being enabled at the same time, mirroring the safety check that was present in the MQL version.

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `CloseAll` | `bool` | `true` | When enabled the strategy flattens any open position as soon as it starts receiving market data. |
| `CloseAllProfit` | `bool` | `false` | When enabled the strategy exits only positions with a positive unrealized profit. |
| `CloseAllLoss` | `bool` | `false` | When enabled the strategy exits only positions with a negative unrealized profit. |

Only one of these switches can be active at the same time. If more than one is selected, the strategy stops after logging an error message.

## Trading Logic
- The strategy subscribes to tick trades using `SubscribeTrades()` and checks the position after every new trade.
- If `CloseAll` is enabled, the current position is closed immediately.
- For the profit and loss modes the strategy calculates a simple unrealized result from the average entry price and the latest trade price. Positive values trigger a close in the profit mode, and negative values trigger a close in the loss mode.
- Before sending market exit orders the strategy cancels any active orders to avoid conflicting instructions.
- A guard flag ensures that only one closing order is sent per position change; it is cleared automatically when the net position becomes flat.

## Implementation Notes
- The conversion keeps the behaviour of the original script where commissions and swaps are assumed to be part of the profit calculation. Because StockSharp provides the average position price, the profit evaluation uses the difference between the current trade price and that average price. This is sufficient to detect the sign of the result.
- All comments in the code are provided in English as required.
- The strategy is meant to manage positions that already exist. It does not generate new entries and can be combined with other strategies or manual trading if necessary.

## Usage Tips
1. Attach the strategy to the security and portfolio that contain the positions you want to manage.
2. Pick exactly one mode (`CloseAll`, `CloseAllProfit`, or `CloseAllLoss`) depending on the desired behaviour.
3. Start the strategy and ensure market data is available so that exit orders can be placed immediately.
4. Monitor the log for confirmation messages such as “Closing profitable position” or “Closing losing position” to verify activity.

## Files
- `CS/JimsClosePositionsStrategy.cs` – the strategy implementation.
- `README.md`, `README_ru.md`, `README_cn.md` – documentation in English, Russian, and Chinese.
