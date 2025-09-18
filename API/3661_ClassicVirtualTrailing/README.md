# Classic & Virtual Trailing Strategy

## Overview
The **Classic & Virtual Trailing Strategy** is a C# conversion of the MetaTrader expert `Classic & Virtual Trailing.mq4` (MQL ID 49326).
Just like the original EA, this implementation does not open trades. Instead it attaches to an existing position and manages the
protective trailing stop according to two selectable modes:

- **Classic** – emulates MetaTrader's native trailing stop by maintaining a real stop level behind price. When the trail is hit
the position is flattened at market, mirroring the behaviour of a server-side stop order.
- **Virtual** – mirrors the "virtual" trailing branch from the source EA. The trail is tracked internally without touching the
exchange stop orders. If price touches the virtual level, the strategy closes the position manually.

Both modes share the same activation rules: the trail starts only after the price has moved `TrailingStartPips` pips beyond the
entry and it always stays `TrailingGapPips` pips behind price afterwards. This port keeps the same MetaTrader pip semantics by
converting inputs to price offsets through the instrument `PriceStep` (with 10x multiplication for 3/5-digit forex symbols).

## Trading logic
1. **Level1 subscription** – the strategy subscribes to level1 data and stores the latest bid/ask quotes. No candles or depth
feeds are required, allowing the manager to react to every tick.
2. **Pip conversion** – runtime parameters are defined in pips. They are converted to price units with the helper `GetPipSize()`
method that reproduces MetaTrader's pip calculation.
3. **Trailing activation** – once the unrealised profit grows by at least `TrailingStartPips + TrailingGapPips`, a trailing level
is created at `Bid − TrailingGap` (long positions) or `Ask + TrailingGap` (short positions).
4. **Stop-level safety** *(Classic mode)* – if the broker exposes a minimal stop level through level1 fields such as
`StopLevel/StopDistance`, the gap is automatically increased to satisfy that restriction before updating the trail.
5. **Trail maintenance** – on every favourable tick the stop level is moved only forward (never backwards). The most recent trail
is stored separately for long and short positions.
6. **Exit execution** – when the market crosses the trailing level, the strategy closes the position with a market order. This
applies to both classic and virtual modes because StockSharp executes stops client-side.
7. **Position resets** – whenever the net position returns to zero, any cached trailing levels are cleared to be ready for the
next trade, matching the EA's loop over individual tickets.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `TrailingMode` | Selects between `Classic` stop-order style trailing and `Virtual` manual closing. | `Virtual` |
| `TrailingStartPips` | Profit in pips that must be reached before the trailing logic activates. | `30` |
| `TrailingGapPips` | Distance in pips maintained between price and the trailing level. | `30` |

Every parameter is declared through `StrategyParam<T>` so they can be optimised inside StockSharp Designer.

## Implementation notes
- The strategy operates on the aggregated net position (`Strategy.Position`). This mirrors how StockSharp handles portfolios and
is the closest analogue to iterating over open orders in MetaTrader.
- Trailing levels are recalculated from the latest bid/ask, therefore the strategy should be attached only to securities that
provide level1 quotes.
- Classic mode honours the broker's minimal stop distance when that information is published via level1 fields. If the
information is not available, the configured gap is used without modification.
- No chart objects are drawn because visual elements such as horizontal lines are handled differently in StockSharp.
- The logic intentionally avoids accessing historical indicator buffers, following the project-wide guidelines for converted
strategies.

## Files
- `CS/ClassicVirtualTrailingStrategy.cs` – strategy implementation.
- `README.md`, `README_cn.md`, `README_ru.md` – multilingual documentation.
