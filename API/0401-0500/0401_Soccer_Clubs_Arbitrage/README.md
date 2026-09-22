# Soccer Clubs Arbitrage
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy compares completed candle closes for two related instruments. It calculates the relative premium as `primary / second - 1` and trades both legs when the absolute premium exceeds the entry threshold.

If the primary instrument is more expensive, the strategy sells it and buys the second instrument with the same unit volume. If the second instrument is more expensive, the directions are reversed. Both positions are closed when the absolute premium falls below the exit threshold.

## Details

- **Data**: Completed candles for the primary security and `Security2Id`; the default timeframe is five minutes.
- **Entry**: Open equal-unit, opposite market orders when the premium exceeds `EntryThreshold` in either direction.
- **Exit**: Flatten the actual position of each leg when the absolute premium is below `ExitThreshold`.
- **Cooldown**: Wait `CooldownBars` paired candle updates after an entry, exit, or reversal before acting again.
- **Execution risk**: The two market orders are submitted separately and are not atomic. Equal units also do not guarantee equal notionals, so legging, liquidity, and contract-size risk remain.

