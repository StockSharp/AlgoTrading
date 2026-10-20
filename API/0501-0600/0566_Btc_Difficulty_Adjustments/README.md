# BTC Difficulty Adjustments

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

Requires an explicit `DifficultyHistoryFile` with header `Time,Difficulty`. Timestamps must be strictly increasing ISO UTC values ending in `Z`; difficulty must be positive. The file is read at startup, and missing or malformed data fails explicitly. No mining series is inferred from market prices.

For each finished candle, consume only observations published at or before its `OpenTime`. The first observation establishes the baseline; unchanged difficulty gives no signal. Change is `100*(difficulty/previous-1)`. With `ThresholdMode=false`, an increase buys and a decrease sells. With threshold mode, only `abs(change)>=Threshold` acts: a decrease buys, an increase sells. The latest eligible observed adjustment chooses the side. Open the configured `Volume` from flat or reverse with `Volume+abs(Position)`. Pending orders block overlapping decisions. There is no automatic stop.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | `1d` |
| `ThresholdMode` | `false` |
| `Threshold` | `10` |
| `DifficultyHistoryFile` | `required` |
