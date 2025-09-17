# Jim's Close Orders Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy converted from the original MetaTrader 4 script that mass-closes positions. The strategy acts immediately after start, evaluates the profit or loss of every open position in the connected portfolio, and sends market orders to flatten the exposure according to the selected closing mode.

## Details

- **Purpose**: emergency liquidation and manual risk management when the entire book must be closed quickly.
- **Scope**: iterates over all open positions in the assigned portfolio, regardless of the configured `Security` property.
- **Workflow**:
  1. On start the strategy checks which closing mode is enabled.
  2. When exactly one mode is active it takes a snapshot of existing positions.
  3. Positions matching the filter are closed with market orders (`ClosePosition`).
  4. The strategy stops itself right after processing the snapshot, mirroring the one-shot nature of the MT4 script.
- **Price source**: profitable / losing filters use the best bid (for long exits) or best ask (for short exits). If those are missing the strategy falls back to the last trade price. Positions without price information are skipped with a warning.
- **No trading**: the strategy never opens new positions or touches pending orders. It only attempts to close currently open positions.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CloseOpenOrders` | `true` | Close every open position, ignoring unrealized PnL. Only one mode can be enabled at a time. |
| `CloseOrdersWithPlusProfit` | `false` | Close only positions whose estimated unrealized PnL is zero or positive. |
| `CloseOrdersWithMinusProfit` | `false` | Close only positions whose estimated unrealized PnL is zero or negative. |

### Parameter Notes

- Exactly one of the three switches must be enabled. When zero or multiple flags are set, the strategy logs a warning and stops without sending any orders.
- Unrealized PnL estimation uses `(exitPrice - averagePrice) * signedVolume` with the exit price chosen according to the position direction.
- Positions lacking a linked `Security` or any usable market price are skipped for safety and left untouched.

## Usage Tips

- Ensure that Level1 (best bid / ask) or trade data is available so that the profit filters can evaluate positions accurately.
- Because the strategy closes positions across the whole portfolio, use a dedicated portfolio or account when selective liquidation is required.
- Launch the strategy only when the connection is online; otherwise the generated market orders will remain pending until connectivity is restored.
- Combine with other supervisory strategies if pending orders must also be cancelled (this conversion intentionally mirrors the original script that handled only open market positions).
