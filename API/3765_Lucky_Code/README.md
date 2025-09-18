# Lucky Code Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Lucky Code is a short-term breakout scalper converted from the original MetaTrader "Lucky_code" expert advisor. The strategy watches the spread extremes and reacts when the best ask jumps above or the best bid falls below the previous quote by a configurable distance. All trades are closed aggressively: profits are taken immediately once price ticks favorably, while losses are cut when an adverse excursion breaches a protective limit.

## Data and execution

- **Market data**: requires a steady stream of Level 1 quotes to read the latest best bid and ask values.
- **Order types**: uses market orders for every entry and exit to mirror the tick-based execution of the MQL version.
- **Position mode**: supports both netting and hedging accounts. Multiple fills accumulate into a single net position that is managed as a block.

## Parameters

- **Shift points** – minimum number of points (pips) between consecutive quotes that unlocks a new entry. Higher values reduce trade frequency and noise sensitivity.
- **Limit points** – maximum adverse distance allowed before positions are force-closed. The value is converted into price units with the instrument tick size.

## Trading logic

1. **Initialization**
   - Converts point-based parameters into real price offsets using the security tick size.
   - Subscribes to Level 1 data and resets the internal buffers for the last seen bid and ask.
2. **Entry rules**
   - When the best ask advances by at least the configured shift above the previous ask, the strategy opens a short position (matching the original EA behavior that sells after upward spikes).
   - When the best bid drops by at least the same shift under the previous bid, the strategy opens a long position to capture the rebound.
3. **Volume sizing**
   - Starts from the strategy `Volume` property.
   - If the portfolio value is available, the size is increased to `round(Equity / 10,000, 1)` lots, emulating the MetaTrader margin-based sizing.
4. **Exit rules**
   - Long exposure is closed immediately once the bid exceeds the average entry price or the ask moves down by the configured loss limit.
   - Short exposure is closed once the ask falls below the entry price or the bid rises above it by the loss limit.

## Implementation notes

- The strategy reacts on every quote update, so consider throttling noisy feeds or increasing the shift parameter in production environments.
- Because market orders are used for both opening and closing trades, ensure sufficient liquidity to avoid slippage spikes during fast quote jumps.
- Additional portfolio-level risk controls (daily stop, maximum drawdown, etc.) are recommended when running the strategy live.
