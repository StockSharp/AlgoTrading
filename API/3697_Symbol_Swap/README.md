# Symbol Swap Strategy

The **Symbol Swap Strategy** is the StockSharp port of the MetaTrader 5 utility "Symbol Swap". The original MQL5 program opens a panel where a trader can enter any ticker, immediately switch the current chart to that symbol, and monitor a compact data window with the latest time, OHLC prices, tick volume, and spread. This C# conversion keeps the same responsibilities while relying exclusively on StockSharp's high-level subscription API.

## Behaviour

1. On start the strategy resolves the instrument to watch. It first tries `WatchedSecurityId`; if the field is empty it falls back to `Strategy.Security` that is configured in the launcher.
2. Candle data of the chosen `CandleType` is streamed through `SubscribeCandles(...)`. Finished bars deliver the open, high, low, close, and tick volume that populate the panel.
3. Real-time best bid/ask values arrive via `SubscribeLevel1(...)`. The spread is recalculated on every quote update to mirror the MQL data window.
4. The formatted block is either written to the strategy log (`OutputMode = Log`) or rendered on a chart (`OutputMode = Chart`) with `DrawText(...)`, recreating the floating panel from MetaTrader.
5. Calling `SwapSecurity("TICKER")` during execution resolves the new security through `SecurityProvider.LookupById` and seamlessly resubscribes both the candle and Level 1 feeds to the requested instrument.

The strategy is informational only; it does not place orders. It can run standalone as a market dashboard or alongside other trading bots.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Time frame that defines the candle subscription used to build OHLC and tick volume data. | `TimeFrame(1 minute)` |
| `WatchedSecurityId` | Optional instrument identifier. Leave empty to use `Strategy.Security`. | _empty_ |
| `OutputMode` | Rendering destination of the information block. Choose between `Chart` (overlay near the price) or `Log` (strategy log). | `Chart` |

## Public methods

| Method | Description |
|--------|-------------|
| `SwapSecurity(string securityId)` | Resolves the provided ticker through the active `SecurityProvider` and immediately switches the panel to that symbol. The method can be called multiple times; each call clears previous candle/Level 1 subscriptions before adding the new feeds. |

## Usage notes

- Ensure the connector exposes the requested identifier; otherwise `SecurityProvider.LookupById` throws an exception.
- When `OutputMode = Chart`, the strategy automatically creates a chart area, draws the subscribed candles, and overlays the status block. For log mode only the textual updates are produced.
- Tick volume equals the candle's `TotalVolume`, which is how MetaTrader reports its per-bar tick count.
- Spread is shown only when both best bid and best ask are available. Otherwise the field displays `n/a`.

## Conversion details

- The MetaTrader timer loop is replaced with StockSharp subscriptions. Candles trigger once per finished bar and Level 1 quotes refresh the spread in real time.
- The MQL panel labels are represented by a single multi-line text block. The text uses the exact ordering from the original tool: Time, Period, Symbol, Close, Open, High, Low, Tick Volume, Spread.
- Runtime symbol swaps no longer need manual Market Watch management—the strategy resolves instruments directly via the StockSharp security provider.
- Only high-level API calls are used (`SubscribeCandles`, `SubscribeLevel1`, `DrawText`, `AddInfo`). There are no manual indicator calculations or direct connector manipulations, satisfying the repository coding rules.

