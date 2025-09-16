# Martingale VI Hybrid Strategy (C#)

## Overview
The Martingale VI Hybrid strategy is a conversion of the original MetaTrader expert advisor into the StockSharp high-level API. It combines a fast/slow moving average filter with a MACD confirmation and scales into positions using a martingale multiplier. The strategy accumulates positions when price moves against the last entry by a fixed pip distance and unifies the take profit of the entire cluster at the level defined by the most recent order. Additional global exits include fixed profit in money, profit as a percentage of starting equity, and a trailing stop in money.

## Trading Logic
1. **Signal filter** – the previous candle values of the fast and slow SMAs and the MACD histogram are used. A long cycle starts when the fast SMA was above the slow SMA and the MACD main line was below its signal line. A short cycle starts when the fast SMA was below the slow SMA while the MACD main line was above the signal line.
2. **Initial position** – when a new cycle starts and no position is open, the strategy sends a market order with the `Initial Volume`.
3. **Martingale additions** – while a position is open, the strategy watches the latest entry price. If price moves against the position by `Pip Step` pips, it adds another market order whose volume is `previous order volume × Volume Multiplier`. The number of active orders is limited by `Max Trades`. When the limit is reached and `Close Max Orders` is enabled, the whole position is closed immediately.
4. **Shared take profit** – every new order updates the common take profit level to `entry price ± Take Profit (pips)` depending on direction. Once the candle’s high (for longs) or low (for shorts) touches this level, all orders are closed together.
5. **Global exits** – the floating profit is continuously evaluated:
   - If `Use Money TP` is enabled and the profit reaches `Money TP`, the position is closed.
   - If `Use Percent TP` is enabled and the profit reaches `Percent TP` percent of the initial portfolio value, the position is closed.
   - If `Enable Trailing` is active, a trailing stop in money is applied once the profit exceeds `Trailing Activation`. The position is closed if the profit falls by `Trailing Drawdown` from the peak.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Candle Type` | Primary candle series used for indicator updates.
| `Fast MA`, `Slow MA` | Periods of the simple moving averages that define the trend filter.
| `MACD Fast`, `MACD Slow`, `MACD Signal` | Parameters of the MACD indicator used for confirmation.
| `Initial Volume` | Volume of the first order in a martingale cycle.
| `Volume Multiplier` | Multiplier applied to the previous order volume for every addition.
| `Max Trades` | Maximum number of simultaneous orders in the martingale sequence.
| `Take Profit (pips)` | Take profit distance for each order; the latest order defines the shared take profit price.
| `Pip Step` | Price move against the current cycle that triggers the next addition.
| `Use Money TP`, `Money TP` | Enables and sets the profit target in account currency.
| `Use Percent TP`, `Percent TP` | Enables and sets the profit target as a percentage of initial portfolio value.
| `Enable Trailing`, `Trailing Activation`, `Trailing Drawdown` | Parameters of the cash-based trailing stop that protects accumulated profit.
| `Close Max Orders` | When enabled, the whole position is closed as soon as the martingale order limit is reached.

## Risk Management
- The strategy supports both absolute and percentage-based profit targets to lock gains early.
- The trailing stop in money prevents the position from giving back more than the configured drawdown after a profitable run.
- Limiting the total number of martingale steps avoids unbounded position growth; enabling `Close Max Orders` forces an emergency exit when the sequence reaches its configured limit.

## Implementation Notes
- The strategy uses the StockSharp high-level `SubscribeCandles` API with indicators bound via `BindEx` for MACD and manual processing for the moving averages.
- Pip size is derived from the security’s price step, including support for 5-digit and 3-digit pricing.
- Profit calculations rely on `Security.PriceStep`, `Security.StepPrice`, and `PositionAvgPrice`, ensuring compatibility with instruments that provide the necessary metadata.
