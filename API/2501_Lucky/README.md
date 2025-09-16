# Lucky Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Lucky strategy is a breakout scalper that monitors rapid changes between the best bid and ask prices. It buys when the ask price jumps upward by a configurable number of pips and sells when the bid falls by the same amount. Positions are closed immediately once they become profitable or if price moves adversely beyond a protective threshold.

## Data and execution

- **Market data**: requires Level 1 quotes to access the best bid and ask stream.
- **Order types**: uses market orders for all entries and exits to react quickly to quote shocks.
- **Position mode**: designed for hedging-style accounts but works with netting accounts by accumulating net exposure.

## Parameters

- **Shift points** – minimum pip distance between consecutive quotes that triggers a new trade. A higher value filters out noise, while a lower value reacts to even tiny jumps.
- **Limit points** – maximum adverse move (in pips) tolerated before force-closing an open position. It also scales with the instrument tick size.
- **Reverse mode** – flips the trading direction. When enabled, upward ask shocks open shorts and downward bid shocks open longs.

## Trade logic

1. **Initialization**
   - Converts the point-based parameters into actual price distances using the instrument tick size.
   - Subscribes to Level 1 data and resets internal buffers for previous bid and ask prices.
2. **Entry**
   - When the ask increases by at least the configured shift relative to the previous ask, the strategy opens a long (or short in reverse mode).
   - When the bid decreases by at least the shift relative to the previous bid, the strategy opens a short (or long in reverse mode).
3. **Volume sizing**
   - Default order quantity comes from the strategy `Volume` property.
   - If portfolio equity is available, it emulates the MetaTrader logic by allocating roughly `FreeMargin / 10,000`, rounded to one decimal lot, ensuring larger accounts trade larger sizes.
4. **Exit**
   - Long positions close as soon as the bid exceeds the average entry price or the ask drops below the entry by the configured limit.
   - Short positions close once the ask falls below the entry or the bid rises above the entry by the limit.

## Notes and usage tips

- Works best on highly liquid FX pairs or index CFDs with noticeable quote jumps.
- Combine with additional risk management such as portfolio-level stop-outs when testing live.
- Enable **Reverse mode** to transform the breakout into a fade strategy without modifying any other parameters.
- Because the strategy reacts to every qualifying quote update, consider throttling incoming data or increasing the shift threshold on noisy feeds.
