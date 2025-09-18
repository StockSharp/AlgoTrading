# Lucky Shift Limit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Lucky Shift Limit** strategy is a direct conversion of the MetaTrader 4 expert advisor `Lucky_acnl6p6j89zn91fa.mq4`. It watches the best bid/ask quotes in real time and reacts to sudden jumps measured in MetaTrader "points" (pips). When the ask price accelerates upward by the configured shift distance the strategy fades the move by selling, while a sharp drop in the bid prompts a contrarian buy. All open trades are constantly monitored and closed either once they become profitable or when the floating loss exceeds a safety threshold identical to the original MQ4 logic.

## Data and execution requirements

- **Market data** – subscribes to Level 1 quotes only; no candles or depth of market are required.
- **Execution style** – entries and exits rely on market orders to mimic the immediate `OrderSend` calls from MetaTrader.
- **Account mode** – works with both hedging and netting accounts. On netting accounts the strategy accumulates exposure in a single position and the exit module flattens it.
- **Volume sizing** – default order size comes from `Strategy.Volume`, but the helper emulates `AccountFreeMargin/10000` from MetaTrader when the portfolio value is available.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `Shift points` | 3 | Minimum number of MetaTrader points between consecutive asks/bids that triggers a new order. Larger values filter out noise, smaller values react faster. |
| `Limit points` | 18 | Maximum adverse excursion allowed for an open trade. If price moves against the position by this many points the trade is force-closed. |

Both parameters are expressed in MetaTrader points and converted internally into absolute price offsets using the instrument tick size. Optimisation boundaries in the UI match the practical ranges from the MQ4 version.

## Trading logic

1. **Initialisation**
   - Converts the point-based settings into actual price distances using `Security.PriceStep`.
   - Resets cached bid/ask quotes and starts a Level 1 subscription with high-level `Bind` processing.
2. **Entry conditions**
   - If the ask rises by at least `Shift points` compared to the previous ask, the strategy sends a market sell order (fading the spike) with a log note explaining the trigger.
   - If the bid falls by at least the same distance compared to the previous bid, it opens a market buy.
   - Signals can fire multiple times in sequence, exactly like the original expert that did not restrict the number of simultaneous positions.
3. **Exit management**
   - Every quote tick invokes `TryClosePosition()`. Long positions are closed immediately when the bid is above the average entry (realised profit) or when the ask is lower than the entry by `Limit points` (loss cap).
   - Short positions mirror this logic, closing on profitable ask quotes or when the bid exceeds the entry by the configured limit.
   - All exits use market orders to replicate `OrderClose` and guarantee the position is flattened on the same tick.
4. **Position sizing**
   - Calculates the default volume from portfolio equity (`equity / 10,000`, rounded to one decimal lot) when available, matching the MQ4 helper `GetLots()`.
   - Falls back to the strategy `Volume` property when equity data is missing.

## Implementation notes

- Uses only high-level StockSharp APIs: `SubscribeLevel1().Bind(ProcessLevel1)` removes the need for manual quote listeners.
- No custom collections are stored; previous bid/ask values are kept in simple nullable variables as permitted by the guidelines.
- The loss cap works with the instrument tick size, so exotic symbols with fractional pip steps automatically map to the correct price delta.
- Parameter changes during runtime are respected—the strategy recalculates thresholds when Level 1 data arrives.
- Logging statements document every entry and exit reason, which simplifies backtesting and live diagnostics.

## Usage tips

- Ideal for highly liquid FX pairs or indices where bid/ask shocks occur frequently.
- Consider pairing the strategy with portfolio-level protections (`StartProtection`) if additional stop loss or drawdown limits are required.
- Increase `Shift points` on noisy feeds to reduce overtrading, or decrease it to capture ultra-short-term moves.
- The logic is inherently contrarian; if breakout behaviour is desired simply set `Shift points` high enough or combine it with another filter indicator.
