# CloseProfit V2 Strategy

## Overview
CloseProfit V2 replicates the behaviour of the original MetaTrader utility that force-closes all active trading exposure once a configurable profit or loss threshold is reached. The StockSharp port acts as an account-protection module: it monitors floating PnL on every completed candle and, when limits are exceeded, cancels outstanding orders and liquidates positions. The strategy is designed to run alongside discretionary or automated entries that rely on the same portfolio.

Unlike signal-generating systems, CloseProfit V2 never opens positions on its own. It simply observes real-time profit and loss metrics, allowing traders to automate the “panic button” logic used in the MQL version. The monitoring frequency is controlled through a candle subscription, which makes the component compatible with both historical backtesting and live trading environments.

## How it works
1. When the strategy starts, it captures the current portfolio value as the last flat equity snapshot and launches the configured candle subscription.
2. Each time a candle finishes, the strategy stores the closing price and evaluates floating profit:
   - If `AllSymbols` is disabled, only the primary security is tracked. Floating profit is calculated as `Position * (lastClose - averagePrice)` so only unrealized PnL is used, mirroring the MQL logic that sums open trades.
   - If `AllSymbols` is enabled, the module compares the current portfolio value with the last flat equity snapshot. This measures the combined unrealized gain/loss across all instruments managed by the strategy.
3. When floating profit exceeds `ProfitClose` or drops below `-LossClose`, the strategy requests a full liquidation. It immediately cancels active orders and sends market instructions to flatten every affected security.
4. After liquidation completes and all positions reach zero, the flat equity snapshot is refreshed. This ensures subsequent monitoring starts from the new account balance and avoids re-triggering on realized profits.

The implementation mirrors the original MQL EA’s behaviour: it ignores historical realized PnL and reacts purely to open positions. A built-in protection block guarantees that the closing routine runs only once per signal and does not repeatedly spam cancellation requests.

## Parameters
- **ProfitClose (default 10)** – Floating profit threshold in account currency. When unrealized gains reach this level, the strategy flattens all monitored positions.
- **LossClose (default 1000)** – Floating loss threshold. Once the unrealized drawdown exceeds this absolute value, every position is closed to stop further losses.
- **AllSymbols (default false)** – If `false`, only the primary `Security` assigned to the strategy is watched. If `true`, the module aggregates floating PnL for every security in the strategy’s position set and liquidates all of them simultaneously.
- **CandleType (default 1-minute time frame)** – Candle series used for evaluation. The candle’s close price drives profit calculations when `AllSymbols` is disabled. A shorter time frame provides faster reactions, while longer frames reduce computational load during backtests.

## Practical notes
- Start the component together with other trading strategies that share the same portfolio. Once thresholds are reached, CloseProfit V2 will cancel their pending orders and close their open positions.
- Commission and swap adjustments are not available in StockSharp’s high-level API, so floating PnL is based purely on price differences. If those costs matter, increase thresholds accordingly.
- Because liquidation relies on market orders, ensure there is sufficient liquidity or slippage buffers when configuring `ProfitClose` and `LossClose`.
- The candle subscription is also used during backtesting to guarantee deterministic evaluation points. In live trading you can switch to faster frames if intrabar monitoring is required.
- The strategy calls `StartProtection()` on startup so StockSharp’s built-in safety checks (e.g., reconnection handling) remain active while the utility is running.

## Differences from the original MQL implementation
- MetaTrader’s “magic number” filter is unnecessary: StockSharp identifies orders by strategy, so the module already isolates positions it controls. `AllSymbols` therefore applies to all securities handled by the same strategy instance.
- The MQL EA managed chart labels to display balance, equity and ticket counts. The C# version uses log messages because StockSharp’s charting is optional and not always available in automated runs.
- Debug/tester scaffolding that auto-created demo trades in MQL was removed. The StockSharp strategy focuses purely on monitoring and liquidation.

## When to use
Deploy CloseProfit V2 whenever a hard stop on floating PnL is needed—whether to protect funded-accounts, enforce proprietary risk policies, or automate session-based profit targets. Adjust the candle period to align with the reaction speed required by your trading workflow.
