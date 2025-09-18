# iTrade Strategy

This strategy is a manual sell manager converted from the MetaTrader expert advisor **iTrade**. It recreates the chart button workflow from the original EA: every time the user requests a sell, a martingale position is opened. The strategy then watches the floating profit of all short trades and liquidates the most and least profitable tickets once predefined profit targets are met.

## Core Logic

- Orders are opened only on explicit user requests. Call `QueueSellRequest()` to simulate the MetaTrader button press.
- The first position uses the configured **Initial Volume**. After every losing trade the next order size is multiplied by the **Martingale Multiplier**. Profitable trades reset the sequence back to the base volume.
- Floating profit is measured using the current best ask price. When the average profit per open trade reaches the **Average Profit Target**, the strategy closes the most profitable and the least profitable trades from the active batch (up to **Base Trade Count** trades).
- If more than **Base Trade Count** positions are open, the stricter **Extended Profit Target** is applied before closing two trades.
- Profit calculations rely on the security `PriceStep` and `StepPrice` values. The strategy throws an exception during start-up when they are missing.

## Parameters

| Name | Description |
| ---- | ----------- |
| `InitialVolume` | Base lot size used for the first martingale order. |
| `MartingaleMultiplier` | Multiplier applied after every losing trade. |
| `AverageProfitTarget` | Average floating profit (in currency) required to close trades within the first batch. |
| `ExtendedAverageProfitTarget` | Average floating profit threshold when more than the base batch is active. |
| `BaseTradeCount` | Number of trades considered part of the initial batch. |
| `ControlInterval` | Frequency of internal checks (timer interval). |

## Usage Notes

1. Set `Security`, `Portfolio` and any desired parameters before starting the strategy.
2. Call `QueueSellRequest()` whenever a new sell should be opened. The strategy will size the order according to the martingale rules and submit a market sell.
3. The algorithm stores a history of closed trade results (up to 200 entries) to reproduce the original martingale behaviour.
4. Closing orders are sent as market buys for the exact volume of the targeted trades.

## Differences from MetaTrader Version

- The MetaTrader version relied on chart buttons; here the user triggers sells programmatically via `QueueSellRequest()`.
- Order execution is handled through StockSharp market orders. Partial fills are aggregated automatically by the strategy.
- Profit thresholds operate on decimal currency values using `StepPrice`, whereas the original EA used MetaTrader ticket profit functions.

