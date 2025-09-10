# Aftershock Playbook Strategy

The **Aftershock Playbook** strategy trades post-earnings drift based on EPS surprises.

- **Entry**: On an earnings release, go long when surprise ≥ `PositiveSurprise` or short when surprise ≤ `NegativeSurprise`. Signals can be reversed with `ReverseSignals`.
- **Stop**: Optional ATR stop (`AtrLength`, `AtrMultiplier`) applied to short positions.
- **Exit**: Optionally close positions after `HoldDays` calendar days (`UseTimeExit`).
- **Re-entry**: After a profitable exit the strategy re-enters once in the same direction. Losing trades block new entries until the next earnings release.

*External earnings data feed is required.*
