# Hedger Strategy

StockSharp port of the MetaTrader 5 expert advisor **hedger.mq5** (MQL #23511). The original system opens a protective hedge in the opposite direction when an existing position draws down by a specified number of pips. Once price retraces by a smaller amount, the hedge is closed even at a loss, allowing the original trade to recover. This conversion reproduces the behaviour with the StockSharp high level API and adapts the mechanics to the platform's net position model.

## Trading logic

1. The strategy monitors the close of every candle from the configured timeframe.
2. For each non-hedge long position it checks whether the distance between the entry price and the current close is greater than or equal to **DrawdownOpenPips**. If no short hedge is active, it opens one with the same volume.
3. For each non-hedge short position it applies the symmetrical rule, opening a long hedge after the loss reaches the open threshold.
4. Active hedge legs are closed when their floating loss reaches **DrawdownClosePips**, mirroring the MetaTrader logic of releasing the protection after a partial recovery.
5. When the account is flat and **StartWithLong** is enabled, the algorithm opens a seed long position to kick off the cycle.

Because StockSharp tracks net positions, the strategy keeps internal ledgers of long and short entries (including which ones are hedges). Each market order updates the ledgers so that hedges can be opened and closed independently even if the broker collapses positions.

## Parameters

| Parameter | Description |
| --- | --- |
| `DrawdownOpenPips` | Drawdown in pips that triggers opening the opposite hedge. |
| `DrawdownClosePips` | Drawdown in pips that forces the hedge to be closed. |
| `InitialVolume` | Volume of the initial trade when seeding the cycle. |
| `StartWithLong` | If enabled, opens the initial long position when flat. |
| `EnableVerboseLogging` | Writes hedging actions to the strategy log for debugging. |
| `CandleType` | Candle series used for monitoring drawdowns. |

## Differences from the MetaTrader version

- The expert advisor relied on ticket comments (`hedge_buy` / `hedge_sell`) to distinguish hedge positions. The conversion stores this state in memory because StockSharp uses netting.
- Margin checks and slippage settings are omitted; order placement uses the high level `BuyMarket` / `SellMarket` helpers.
- The strategy exposes optimisation ranges for the pip thresholds and volume so they can be tuned with StockSharp optimisers.

## Usage notes

1. Attach the strategy to the desired symbol and portfolio.
2. Adjust the pip thresholds to match the instrument's volatility.
3. Enable verbose logging when validating the conversion—the log records every hedge creation and removal with pip statistics.
4. Deploy on timeframes that deliver meaningful candle closes (e.g. M15 to H1) to avoid overtrading.
