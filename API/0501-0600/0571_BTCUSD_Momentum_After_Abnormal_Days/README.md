# BTCUSD Momentum After Abnormal Days

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

For a finished daily candle, return is `r=(Close-Open)/Open`. Compute the mean and population deviation from the prior `LookbackPeriod` completed daily returns, excluding the current return. A full prior window is required. Buy if `r>mean+k*sd`, sell if `r<mean-k*sd`, with `k=AbnormalThreshold`. Equal thresholds do not enter.

Enter only while flat. Quantity is `floor(CapitalPerTrade/Close/VolumeStep)*VolumeStep`; skip quantities below the instrument minimum and reject those above its maximum. Capital is measured at the signal close, so actual market execution may differ. Close the entire filled position at the following daily candle's close, or the first available later daily close after a gap. Exit has priority over another entry on that candle. Pending orders block duplicates; no price stop is configured.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | `1d` |
| `LookbackPeriod` | `5` |
| `AbnormalThreshold` | `1.6` |
| `CapitalPerTrade` | `1000` |
