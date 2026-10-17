# Demo GPT - Day Trading Scalping Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Enters long after two consecutive green candles when the second candle opens above the first one's close and volume exceeds its 20-bar average. Closes the position on two consecutive red candles where the second opens below the first one's close under the same volume condition. Calculates EMA(20), VWAP, and Camarilla levels for reference.

### Executed conditions and defaults

Defaults: `EmaLength=20`, `VolumeLookback=20`, `CandleType=1 minute`. After at least 20 **previous finished candles** are stored, define `volumeHigh = current.TotalVolume > average(previous 20 TotalVolumes)` (current volume is excluded from its own average). From flat, enter **long only** when both previous and current candles satisfy `Close > Open`, **current `Open > previous Close`**, and `volumeHigh`. Close the **entire actual long position** when both candles satisfy `Close < Open`, current `Open < previous Close`, and `volumeHigh`. A pattern without its strict opening gap or volume confirmation is **not a signal**, and no short position is ever opened. For reference only (not signal gating), `EMA20` uses completed closes; each day's VWAP is `sum(typical price × volume) / sum(volume)`, resetting daily; prior-day Camarilla `H3/L3 = previous close ± 1.1 × (previous high − previous low) / 4`. These reference values are calculated but are not additional trading filters. No independent stop-loss is supplied.

