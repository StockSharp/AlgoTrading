# RRS Non-Directional Strategy

## Overview
This strategy ports the MetaTrader 4 expert advisor "RRS Non-Directional" to the StockSharp framework. The original EA opens hedged buy and sell baskets depending on the selected trading mode and manages them with virtual stop-loss, take-profit and trailing rules. The StockSharp implementation reproduces the configurable modes, money-risk shutdown and virtual protective logic while adapting the behaviour to the netting portfolios used by StockSharp. Hedge-based modes therefore alternate between long and short exposure instead of keeping simultaneous opposite positions.

## Trading logic
- Subscribe to Level-1 data in order to read the best bid/ask prices. The spread reported by those quotes is compared against `MaxSpreadPoints` before every entry decision.
- Market entries respect the `TradingMode` parameter:
  - `HedgeStyle` and `AutoSwap` mirror the dual-sided mode by alternating between long and short trades (StockSharp cannot hold independent buy and sell tickets simultaneously).
  - `BuySellRandom` flips a coin on each new opportunity.
  - `BuySell` always opens the opposite side of the most recently closed position.
  - `BuyOrder` and `SellOrder` restrict trading to a single direction.
- The `New_Trade` extern is mapped to `AllowNewTrades`, providing a quick way to pause all new market orders.
- Every order uses the configured `TradeVolume` and attaches the `TradeComment` for easier tracking on the broker side.

## Risk management and exits
- Stop-loss and take-profit distances are expressed in MetaTrader points. They are converted to price units using the instrument `PriceStep` so that the logic remains broker-agnostic.
- `StopMode`, `TakeMode` and `TrailingMode` select between disabled, virtual and classic management. In the StockSharp port both non-disabled modes are implemented as virtual checks that close the position via market orders when the threshold is reached. This keeps the behaviour deterministic across connectors.
- Trailing management activates after price advances by `TrailingStartPoints`, then maintains a dynamic stop that trails the best price by `TrailingGapPoints`.
- Unrealised profit and loss is recomputed on every Level-1 update. When it falls below the threshold derived from `RiskMode` and `MoneyInRisk`, the strategy liquidates the position immediately.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradingMode` | Entry selection copied from the original EA. Hedge modes alternate between long and short trades under StockSharp's netting model. |
| `AllowNewTrades` | Enables or disables new market orders. |
| `TradeVolume` | Base size for orders. |
| `StopMode` | Stop-loss handling (`Disabled`, `Virtual`, `Classic`). |
| `StopLossPoints` | Stop-loss distance in MetaTrader points. |
| `TakeMode` | Take-profit handling (`Disabled`, `Virtual`, `Classic`). |
| `TakeProfitPoints` | Take-profit distance in MetaTrader points. |
| `TrailingMode` | Trailing stop management (`Disabled`, `Virtual`, `Classic`). |
| `TrailingStartPoints` | Profit (points) required before the trailing stop arms. |
| `TrailingGapPoints` | Distance (points) maintained behind the best price once trailing is active. |
| `RiskMode` | Interprets `MoneyInRisk` either as a balance percentage or as an absolute currency amount. |
| `MoneyInRisk` | Risk amount or percent that triggers a full liquidation when floating P&L falls below the threshold. |
| `MaxSpreadPoints` | Maximum spread (points) allowed for new trades. |
| `SlippagePoints` | Informational slippage setting kept for parity with the original inputs. |
| `TradeComment` | Comment attached to every order. |

## Notes and limitations
- AutoSwap relies on swap-rate information in MetaTrader. StockSharp connectors usually do not expose those figures via Level-1 feeds, so the mode falls back to `HedgeStyle` and logs the downgrade.
- Classic stop-loss, take-profit and trailing options are executed virtually. Brokers that require native protective orders should be handled by lower-level strategy overrides.
- Because StockSharp aggregates positions per security, the strategy alternates the exposure in hedging modes instead of keeping two simultaneous tickets. This behaviour is documented so that forward tests match expectations.
